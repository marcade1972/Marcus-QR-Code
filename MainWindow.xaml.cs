using Microsoft.Win32;
using QRCoder;
using SkiaSharp;
using System;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace MarcusQRCodeVaultClean;

public partial class MainWindow : Window
{
    private string? logoPath;
    private byte[]? lastQr400;

    private TextBox urlTextBox = null!;
    private ComboBox colorComboBox = null!;
    private Image logoPreview = null!;
    private Image qrPreview = null!;

    private const string PlaceholderText = "https://www.example.com";

    public MainWindow()
    {
        InitializeComponent();

        Title = "Marcus' QR Code Generator";
        Width = 1280;
        Height = 850;
        WindowStartupLocation = WindowStartupLocation.CenterScreen;

        BuildInterface();
    }

    private void BuildInterface()
    {
        Grid root = new();

        string imagePath = Path.Combine(
            AppDomain.CurrentDomain.BaseDirectory,
            "Assets",
            "qr-vault-skin.png"
        );

        if (File.Exists(imagePath))
        {
            root.Background = new ImageBrush
            {
                ImageSource = new BitmapImage(new Uri(imagePath, UriKind.Absolute)),
                Stretch = Stretch.Fill
            };
        }
        else
        {
            MessageBox.Show($"Background image not found:\n{imagePath}");
            root.Background = Brushes.Black;
        }

        StackPanel panel = new()
        {
            Width = 390,
            Margin = new Thickness(0, 20, 45, 20),
            HorizontalAlignment = HorizontalAlignment.Right,
            VerticalAlignment = VerticalAlignment.Center,
            Background = new SolidColorBrush(Color.FromArgb(230, 5, 5, 5))
        };

        panel.Children.Add(Label("Enter Hyperlink"));

        urlTextBox = new TextBox
        {
            Height = 34,
            FontSize = 16,
            Text = PlaceholderText,
            Foreground = Brushes.Gray,
            VerticalContentAlignment = VerticalAlignment.Center,
            Margin = new Thickness(15, 0, 15, 10)
        };

        urlTextBox.GotFocus += (s, e) =>
        {
            if (urlTextBox.Text == PlaceholderText)
            {
                urlTextBox.Text = "";
                urlTextBox.Foreground = Brushes.Black;
            }
        };

        urlTextBox.LostFocus += (s, e) =>
        {
            if (string.IsNullOrWhiteSpace(urlTextBox.Text))
            {
                urlTextBox.Text = PlaceholderText;
                urlTextBox.Foreground = Brushes.Gray;
            }
        };

        panel.Children.Add(urlTextBox);

        panel.Children.Add(Label("Choose QR Color"));

        colorComboBox = new ComboBox
        {
            Height = 34,
            FontSize = 16,
            Margin = new Thickness(15, 0, 15, 10),
            SelectedIndex = 0
        };

        AddColor("Black", "#000000");
        AddColor("Red", "#FF0000");
        AddColor("Blue", "#0066FF");
        AddColor("Green", "#00AA33");
        AddColor("Yellow", "#D6A600");
        AddColor("Purple", "#8000FF");
        AddColor("Orange", "#FF8800");
        AddColor("Pink", "#FF3BA7");
        AddColor("Cyan", "#00CFFF");
        AddColor("White", "#FFFFFF");

        panel.Children.Add(colorComboBox);

        Button uploadButton = MakeButton("Upload Logo");
        uploadButton.Margin = new Thickness(15, 5, 15, 6);
        uploadButton.Click += UploadLogo_Click;
        panel.Children.Add(uploadButton);

        logoPreview = new Image
        {
            Width = 80,
            Height = 80,
            Stretch = Stretch.Uniform,
            Margin = new Thickness(10, 0, 10, 6)
        };

        panel.Children.Add(logoPreview);

        Button generateButton = MakeButton("Generate QR Code");
        generateButton.Background = Brushes.DarkRed;
        generateButton.Margin = new Thickness(15, 6, 15, 6);
        generateButton.Click += GenerateQr_Click;
        panel.Children.Add(generateButton);

        Border qrBorder = new()
        {
            Width = 165,
            Height = 165,
            Background = Brushes.WhiteSmoke,
            BorderBrush = Brushes.Red,
            BorderThickness = new Thickness(2),
            Margin = new Thickness(10, 0, 10, 5)
        };

        qrPreview = new Image
        {
            Stretch = Stretch.Uniform
        };

        qrBorder.Child = qrPreview;
        panel.Children.Add(qrBorder);

        Button download400Button = MakeButton("Download 400 x 400 PNG");
        download400Button.Margin = new Thickness(15, 3, 15, 5);
        download400Button.Click += Download400_Click;
        panel.Children.Add(download400Button);

        Button download1000Button = MakeButton("Download 1000 x 1000 PNG");
        download1000Button.Margin = new Thickness(15, 3, 15, 5);
        download1000Button.Click += Download1000_Click;
        panel.Children.Add(download1000Button);

        Button clearButton = MakeButton("Clear Form");
        clearButton.Background = Brushes.DimGray;
        clearButton.Margin = new Thickness(15, 3, 15, 8);
        clearButton.Click += ClearForm_Click;
        panel.Children.Add(clearButton);

        root.Children.Add(panel);
        Content = root;
    }

    private TextBlock Label(string text)
    {
        return new TextBlock
        {
            Text = text,
            Foreground = Brushes.White,
            FontSize = 16,
            FontWeight = FontWeights.Bold,
            Margin = new Thickness(15, 7, 15, 4)
        };
    }

    private Button MakeButton(string text)
    {
        return new Button
        {
            Content = text,
            Height = 38,
            FontSize = 15,
            FontWeight = FontWeights.Bold,
            Foreground = Brushes.White,
            Background = Brushes.Firebrick,
            BorderBrush = Brushes.Red,
            BorderThickness = new Thickness(1)
        };
    }

    private void AddColor(string name, string hex)
    {
        colorComboBox.Items.Add(new ComboBoxItem
        {
            Content = name,
            Tag = hex
        });
    }

    private void UploadLogo_Click(object sender, RoutedEventArgs e)
    {
        OpenFileDialog dialog = new()
        {
            Filter = "Image Files|*.png;*.jpg;*.jpeg;*.bmp;*.webp"
        };

        if (dialog.ShowDialog() == true)
        {
            logoPath = dialog.FileName;
            logoPreview.Source = new BitmapImage(new Uri(logoPath));
        }
    }

    private void GenerateQr_Click(object sender, RoutedEventArgs e)
    {
        string url = urlTextBox.Text.Trim();

        if (url == PlaceholderText)
            url = "";

        if (string.IsNullOrWhiteSpace(url))
        {
            MessageBox.Show("Please enter a hyperlink first.");
            return;
        }

        lastQr400 = GenerateQrCode(url, GetSelectedColorHex(), 400, logoPath);
        qrPreview.Source = LoadImage(lastQr400);
    }

    private void Download400_Click(object sender, RoutedEventArgs e)
    {
        if (lastQr400 == null)
        {
            MessageBox.Show("Generate a QR code first.");
            return;
        }

        SavePng(lastQr400, "marcus-qr-code-400x400.png");
    }

    private void Download1000_Click(object sender, RoutedEventArgs e)
    {
        string url = urlTextBox.Text.Trim();

        if (url == PlaceholderText)
            url = "";

        if (string.IsNullOrWhiteSpace(url))
        {
            MessageBox.Show("Please enter a hyperlink first.");
            return;
        }

        byte[] largeQr = GenerateQrCode(url, GetSelectedColorHex(), 1000, logoPath);
        SavePng(largeQr, "marcus-qr-code-1000x1000.png");
    }

    private void ClearForm_Click(object sender, RoutedEventArgs e)
    {
        urlTextBox.Text = PlaceholderText;
        urlTextBox.Foreground = Brushes.Gray;
        colorComboBox.SelectedIndex = 0;
        logoPreview.Source = null;
        qrPreview.Source = null;
        logoPath = null;
        lastQr400 = null;
    }

    private string GetSelectedColorHex()
    {
        if (colorComboBox.SelectedItem is ComboBoxItem item && item.Tag is string hex)
            return hex;

        return "#000000";
    }

    private static byte[] GenerateQrCode(string url, string colorHex, int size, string? logoFilePath)
    {
        SKColor qrColor = ParseHexColor(colorHex);

        using QRCodeGenerator generator = new();
        using QRCodeData qrData = generator.CreateQrCode(url, QRCodeGenerator.ECCLevel.H);

        int quietZone = size / 16;
        int moduleCount = qrData.ModuleMatrix.Count;
        float moduleSize = (size - quietZone * 2f) / moduleCount;

        using SKBitmap bitmap = new(size, size, true);
        using SKCanvas canvas = new(bitmap);

        canvas.Clear(SKColors.Transparent);

        using SKPaint paint = new()
        {
            Color = qrColor,
            IsAntialias = false,
            Style = SKPaintStyle.Fill
        };

        for (int y = 0; y < moduleCount; y++)
        {
            for (int x = 0; x < moduleCount; x++)
            {
                if (qrData.ModuleMatrix[y][x])
                {
                    canvas.DrawRect(
                        quietZone + x * moduleSize,
                        quietZone + y * moduleSize,
                        moduleSize + 0.5f,
                        moduleSize + 0.5f,
                        paint
                    );
                }
            }
        }

        if (!string.IsNullOrWhiteSpace(logoFilePath) && File.Exists(logoFilePath))
        {
            using SKBitmap? logoBitmap = SKBitmap.Decode(logoFilePath);

            if (logoBitmap != null)
            {
                int logoSize = size / 4;
                int logoX = (size - logoSize) / 2;
                int logoY = (size - logoSize) / 2;

                canvas.DrawBitmap(
                    logoBitmap,
                    new SKRect(logoX, logoY, logoX + logoSize, logoY + logoSize)
                );
            }
        }

        using SKImage image = SKImage.FromBitmap(bitmap);
        using SKData data = image.Encode(SKEncodedImageFormat.Png, 100);

        return data.ToArray();
    }

    private static SKColor ParseHexColor(string hex)
    {
        hex = hex.Replace("#", "");

        if (hex.Length != 6)
            return SKColors.Black;

        byte r = Convert.ToByte(hex[..2], 16);
        byte g = Convert.ToByte(hex.Substring(2, 2), 16);
        byte b = Convert.ToByte(hex.Substring(4, 2), 16);

        return new SKColor(r, g, b);
    }

    private static BitmapImage LoadImage(byte[] imageData)
    {
        BitmapImage image = new();

        using MemoryStream stream = new(imageData);

        image.BeginInit();
        image.CacheOption = BitmapCacheOption.OnLoad;
        image.StreamSource = stream;
        image.EndInit();
        image.Freeze();

        return image;
    }

    private static void SavePng(byte[] data, string defaultFileName)
    {
        SaveFileDialog dialog = new()
        {
            Filter = "PNG Image|*.png",
            FileName = defaultFileName
        };

        if (dialog.ShowDialog() == true)
        {
            File.WriteAllBytes(dialog.FileName, data);
            MessageBox.Show("QR code saved successfully!");
        }
    }
}