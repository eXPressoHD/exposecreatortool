namespace LicenseGenerator;

public sealed class MainForm : Form
{
   private readonly Label _keyStatus = new() { AutoSize = true, MaximumSize = new Size(640, 0) };
   private readonly TextBox _name = new() { Width = 420 };
   private readonly TextBox _email = new() { Width = 420 };
   private readonly TextBox _id = new() { Width = 420, Font = new Font("Consolas", 12) };
   private readonly TextBox _order = new() { Width = 420 };
   private readonly TextBox _output = new() { Multiline = true, ReadOnly = true, Width = 640, Height = 110, Font = new Font("Consolas", 9), ScrollBars = ScrollBars.Vertical };
   private readonly Button _create = new() { Text = "Lizenz erstellen", AutoSize = true };
   private string _lastLicense = "";

   public MainForm()
   {
      Text = "Exposé Creator – Lizenz-Generator (intern)";
      Font = new Font("Segoe UI", 10);
      AutoSize = true;
      AutoSizeMode = AutoSizeMode.GrowAndShrink;
      FormBorderStyle = FormBorderStyle.FixedSingle;
      MaximizeBox = false;
      StartPosition = FormStartPosition.CenterScreen;
      Padding = new Padding(16);

      var layout = new FlowLayoutPanel { FlowDirection = FlowDirection.TopDown, AutoSize = true, WrapContents = false };
      Controls.Add(layout);

      // Schlüssel
      layout.Controls.Add(Header("1. Signaturschlüssel"));
      layout.Controls.Add(_keyStatus);
      var keyButtons = Row();
      keyButtons.Controls.Add(MakeButton("Schlüsselpaar erzeugen …", CreateKey_Click));
      keyButtons.Controls.Add(MakeButton("Öffentlichen Schlüssel kopieren", CopyPublicKey_Click));
      keyButtons.Controls.Add(MakeButton("Ordner öffnen", (_, _) =>
      {
         Directory.CreateDirectory(Licensing.Folder);
         System.Diagnostics.Process.Start("explorer.exe", $"\"{Licensing.Folder}\"");
      }));
      layout.Controls.Add(keyButtons);

      // Lizenz
      layout.Controls.Add(Header("2. Lizenz ausstellen"));
      layout.Controls.Add(Field("Name / Firma *", _name));
      layout.Controls.Add(Field("E-Mail", _email));
      layout.Controls.Add(Field("Installations-ID *", _id));
      layout.Controls.Add(Field("Bestellnummer / Notiz", _order));
      _create.Click += Create_Click;
      layout.Controls.Add(_create);

      layout.Controls.Add(Header("3. An den Kunden schicken"));
      layout.Controls.Add(_output);
      var outButtons = Row();
      outButtons.Controls.Add(MakeButton("Schlüssel kopieren", (_, _) => { if (_lastLicense != "") Clipboard.SetText(_lastLicense); }));
      outButtons.Controls.Add(MakeButton("Als Lizenzdatei speichern …", SaveFile_Click));
      layout.Controls.Add(outButtons);

      layout.Controls.Add(new Label
      {
         AutoSize = true, MaximumSize = new Size(640, 0), ForeColor = Color.DimGray,
         Text = "Der private Schlüssel liegt in Dokumente\\ExposeCreator-Lizenzierung. Sicher aufbewahren und sichern – " +
                "ohne ihn können keine passenden Lizenzen mehr ausgestellt werden. Niemals weitergeben oder ins Repository legen."
      });

      UpdateKeyStatus();
   }

   private void UpdateKeyStatus()
   {
      _keyStatus.Text = Licensing.HasKey
         ? "✔ Schlüssel vorhanden: " + Licensing.PrivateKeyPath
         : "✖ Noch kein Schlüssel. Einmalig „Schlüsselpaar erzeugen“ klicken und den öffentlichen Schlüssel in " +
           "ExporseCreator\\Services\\LicensePublicKey.cs eintragen.";
      _create.Enabled = Licensing.HasKey;
   }

   private void CreateKey_Click(object? sender, EventArgs e)
   {
      if (Licensing.HasKey && MessageBox.Show(this,
             "Es gibt bereits einen Schlüssel. Ein neuer Schlüssel macht ALLE bisher ausgestellten Lizenzen ungültig, " +
             "sobald die App mit dem neuen öffentlichen Schlüssel ausgeliefert wird.\n\nWirklich neu erzeugen?",
             "Achtung", MessageBoxButtons.YesNo, MessageBoxIcon.Warning) != DialogResult.Yes) return;

      var publicKey = Licensing.CreateKeyPair();
      Clipboard.SetText(publicKey);
      UpdateKeyStatus();
      MessageBox.Show(this,
         "Schlüsselpaar erzeugt. Der öffentliche Schlüssel ist in der Zwischenablage.\n\n" +
         "Trage ihn in ExporseCreator\\Services\\LicensePublicKey.cs bei „Value“ ein und baue die App neu.\n\n" +
         "Sichere die Datei private-key.pem (z. B. auf einem USB-Stick oder in einem Passwortmanager).",
         "Fertig", MessageBoxButtons.OK, MessageBoxIcon.Information);
   }

   private void CopyPublicKey_Click(object? sender, EventArgs e)
   {
      if (!Licensing.HasKey) return;
      Clipboard.SetText(Licensing.PublicKey());
      MessageBox.Show(this, "Öffentlicher Schlüssel kopiert.", "Kopiert");
   }

   private void Create_Click(object? sender, EventArgs e)
   {
      if (string.IsNullOrWhiteSpace(_name.Text))
      {
         MessageBox.Show(this, "Bitte Name oder Firma angeben.", "Fehlt");
         return;
      }
      if (!Licensing.IsValidId(_id.Text))
      {
         MessageBox.Show(this, "Die Installations-ID ist ungültig. Sie hat 16 Zeichen, z. B. 4XSP-1NHS-45S2-2MZT.", "Ungültig");
         return;
      }
      try
      {
         _lastLicense = Licensing.Create(_name.Text, _email.Text, _id.Text, _order.Text);
         _output.Text = _lastLicense;
         _id.Text = Licensing.Format(_id.Text);
      }
      catch (Exception ex)
      {
         MessageBox.Show(this, ex.Message, "Fehler", MessageBoxButtons.OK, MessageBoxIcon.Error);
      }
   }

   private void SaveFile_Click(object? sender, EventArgs e)
   {
      if (_lastLicense == "") return;
      using var dialog = new SaveFileDialog
      {
         Filter = "Exposé-Creator-Lizenz (*.lic)|*.lic",
         FileName = "ExposeCreator-Lizenz-" + string.Concat(_name.Text.Split(Path.GetInvalidFileNameChars())).Replace(' ', '_') + ".lic"
      };
      if (dialog.ShowDialog(this) == DialogResult.OK) File.WriteAllText(dialog.FileName, _lastLicense);
   }

   private static Label Header(string text) => new()
   {
      Text = text, AutoSize = true, Font = new Font("Segoe UI Semibold", 12), Margin = new Padding(0, 14, 0, 4)
   };

   private static FlowLayoutPanel Row() => new() { FlowDirection = FlowDirection.LeftToRight, AutoSize = true, WrapContents = false };

   private static Control Field(string label, TextBox box)
   {
      var row = Row();
      row.Controls.Add(new Label { Text = label, Width = 200, TextAlign = ContentAlignment.MiddleLeft, Height = box.Height });
      row.Controls.Add(box);
      return row;
   }

   private static Button MakeButton(string text, EventHandler onClick)
   {
      var b = new Button { Text = text, AutoSize = true, Margin = new Padding(0, 0, 8, 0) };
      b.Click += onClick;
      return b;
   }
}
