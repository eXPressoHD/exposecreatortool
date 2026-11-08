using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace LicenseGenerator;

/// <summary>Muss zum LicenseService der App passen (Format EXC1.payload.signature, ECDSA P-256 / SHA-256).</summary>
internal static class Licensing
{
   public const string Prefix = "EXC1";
   private const string Alphabet = "0123456789ABCDEFGHJKMNPQRSTVWXYZ";

   public static string Folder =>
      Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "ExposeCreator-Lizenzierung");
   public static string PrivateKeyPath => Path.Combine(Folder, "private-key.pem");
   public static string RegisterPath => Path.Combine(Folder, "ausgestellte-lizenzen.csv");

   public static bool HasKey => File.Exists(PrivateKeyPath);

   public static string CreateKeyPair()
   {
      Directory.CreateDirectory(Folder);
      using var ecdsa = ECDsa.Create(ECCurve.NamedCurves.nistP256);
      File.WriteAllText(PrivateKeyPath, ecdsa.ExportPkcs8PrivateKeyPem());
      return Convert.ToBase64String(ecdsa.ExportSubjectPublicKeyInfo());
   }

   public static string PublicKey()
   {
      using var ecdsa = LoadKey();
      return Convert.ToBase64String(ecdsa.ExportSubjectPublicKeyInfo());
   }

   private static ECDsa LoadKey()
   {
      var ecdsa = ECDsa.Create();
      ecdsa.ImportFromPem(File.ReadAllText(PrivateKeyPath));
      return ecdsa;
   }

   public static string Normalize(string id) =>
      new string(id.ToUpperInvariant().Where(char.IsLetterOrDigit).Select(c => c switch { 'O' => '0', 'I' or 'L' => '1', _ => c }).ToArray());

   public static bool IsValidId(string id)
   {
      var n = Normalize(id);
      return n.Length == 16 && n.All(c => Alphabet.Contains(c));
   }

   public static string Format(string id)
   {
      var n = Normalize(id);
      return string.Join("-", Enumerable.Range(0, 4).Select(i => n.Substring(i * 4, 4)));
   }

   public static string Create(string name, string email, string installationId, string order)
   {
      var payload = JsonSerializer.SerializeToUtf8Bytes(new Dictionary<string, string>
      {
         ["n"] = name.Trim(),
         ["e"] = email.Trim(),
         ["m"] = Format(installationId),
         ["t"] = "full",
         ["i"] = DateTime.Today.ToString("yyyy-MM-dd"),
         ["o"] = order.Trim(),
      });

      using var ecdsa = LoadKey();
      var signature = ecdsa.SignData(payload, HashAlgorithmName.SHA256);

      // Selbsttest mit dem öffentlichen Schlüssel
      using var verify = ECDsa.Create();
      verify.ImportSubjectPublicKeyInfo(ecdsa.ExportSubjectPublicKeyInfo(), out _);
      if (!verify.VerifyData(payload, signature, HashAlgorithmName.SHA256))
         throw new CryptographicException("Selbsttest der Signatur fehlgeschlagen.");

      var license = $"{Prefix}.{B64Url(payload)}.{B64Url(signature)}";
      AppendRegister(name, email, Format(installationId), order);
      return license;
   }

   private static void AppendRegister(string name, string email, string id, string order)
   {
      Directory.CreateDirectory(Folder);
      bool header = !File.Exists(RegisterPath);
      using var w = new StreamWriter(RegisterPath, append: true, Encoding.UTF8);
      if (header) w.WriteLine("Datum;Name;E-Mail;Installations-ID;Bestellnummer");
      w.WriteLine(string.Join(";", new[] { DateTime.Now.ToString("yyyy-MM-dd HH:mm"), name, email, id, order }
         .Select(v => "\"" + v.Replace("\"", "\"\"") + "\"")));
   }

   private static string B64Url(byte[] data) =>
      Convert.ToBase64String(data).TrimEnd('=').Replace('+', '-').Replace('/', '_');
}
