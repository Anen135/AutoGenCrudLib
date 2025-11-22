using QRCoder;

namespace AutoGenCrudLib.Views;

public class EntityDetailQRPage<T> : EntityDetailPage<T> where T : Models.EntityBase, new()
{
    public EntityDetailQRPage(T entity) : base(entity) { }

    public override View BuildView()
    {
        var root = base.BuildView() as ScrollView;
        var layout = root.Content as VerticalStackLayout;

        var qrButton = new Button
        {
            Text = "Show QR code",
            BackgroundColor = Colors.LightGray,
            Padding = 10
        };

        qrButton.Clicked += async (_, __) => await ShowQRcode();

        layout.Add(qrButton);

        return root;
    }

    private async Task ShowQRcode()
    {
        // 1. Формируем полезную нагрузку
        string entityType = typeof(T).Name;
        string qrPayload = $"myapp://entity/{entityType}/{Entity.Id}";

        // 2. Генерируем QR
        var qrGenerator = new QRCodeGenerator();
        var qrData = qrGenerator.CreateQrCode(qrPayload, QRCodeGenerator.ECCLevel.Q);
        var pngQr = new PngByteQRCode(qrData);
        byte[] qrBytes = pngQr.GetGraphic(10);

        // 3. Показываем QR в Popup или как ContentPage
        var image = new Image
        {
            Source = ImageSource.FromStream(() => new MemoryStream(qrBytes)),
            WidthRequest = 300,
            HeightRequest = 300,
            Margin = 20
        };

        var popupPage = new ContentPage
        {
            Title = "QR code",
            Content = new VerticalStackLayout
            {
                Padding = 20,
                Children =
                {
                    new Label {
                        Text = "Scan this QR code",
                        FontSize = 20,
                        HorizontalOptions = LayoutOptions.Center
                    },
                    image,
                    new Label {
                        Text = qrPayload,
                        FontSize = 10,
                        HorizontalOptions = LayoutOptions.Center
                    }
                }
            }
        };

        await Navigation.PushAsync(popupPage);
    }
}
