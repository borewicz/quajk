using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Shapes;
using Microsoft.Phone.Controls;
using HtmlAgilityPack; //baardzo popularna biblioteka parsująca pliki HTML - dostępna z NuGeta
using System.Windows.Media.Imaging;
using ShakeGestures; //biblioteka pochodząca z przykładów z SDK
using Microsoft.Phone.Shell;

namespace quajk
{
    public partial class MainPage : PhoneApplicationPage
    {
        ProgressIndicator indicator = new ProgressIndicator(); //tworzenie indicatora na tym pasku do góry - co pokazuje "Losowanie"

        // Constructor
        public MainPage()
        {
            InitializeComponent();
            //ShakeGesturesHelper - obsługa trzęsoty, pochodzi z przykładu sdkShakeGestureLibraryCS w przykładach w dokumentacji Microsoft SDK
            ShakeGesturesHelper.Instance.ShakeGesture +=
                new EventHandler<ShakeGestureEventArgs>(ApplicationBarIconButton_Click); //przypisanie funkcji ApplicationBarButtonClick do zdarzenia trzęsienia telefonem
            ShakeGesturesHelper.Instance.MinimumRequiredMovesForShake = 3; //minimalna ilość potrząśniecia telefonem, aby wykonał funkcję ApplicationBarButtonCLick
            ShakeGesturesHelper.Instance.Active = true; //uruchomienie obsługi trząśnięć
            SystemTray.SetProgressIndicator(this, indicator); //przypisanie paskowi systemowemu naszego indicatora pokazującego tekst
            indicator.Text = "Losowanie..."; //ustawienie tekstu na indicatorze
            indicator.IsVisible = false; //ustawienie widoczności indicatora - jest nam tylko potrzebny przy pobieraniu strony
        }

        protected override void OnNavigatedTo(System.Windows.Navigation.NavigationEventArgs e) //przeciążenie funkcji onNavigate - wykonuje się w czasie pokazywania głównej formatki Page (czyli tym głównym ekranie)
        {
            WebClient client = new WebClient(); //tworzenie nowego klienta 
            client.DownloadStringCompleted += client_DownloadStringCompleted; //przypisanie funkcji client_DownloadStringCompleted do clienta - wykona się po zakończeniu pobierania
            indicator.IsVisible = true; //pokazanie indicatora
            client.DownloadStringAsync(new Uri("http://kwejk.pl/losuj/")); //pobranie strony w formie stringa
        }

        void client_DownloadStringCompleted(object sender, DownloadStringCompletedEventArgs e) //sender to obiekt, z którego została wykonana funkcja
        {
            try
            {
                HtmlDocument doc = new HtmlDocument(); //utworzenie parsera HTML (trzeba wydobyć adres obrazka z źródła HTML)
                doc.LoadHtml(e.Result); //załadowanie do parsera wyniku pobierania (czyli czystego HTML)
                foreach (HtmlNode link in doc.DocumentNode.SelectNodes("//div[@class='media']/a/img")) //dla każdego elementu, który znajduje się w hierarchii HTML podanej w cudzysłowie
                {
                    HtmlAttribute att = link.Attributes["src"]; //pobiera atrybut src z img
                    if (!att.Value.Contains(".gif")) //jeżeli obrazek nie jest gifem
                    {
                        Deployment.Current.Dispatcher.BeginInvoke(() => //wszelkie funkcje działające asynchroniczne zmieniające kontrolki na Page'u (tym ekranie) muszą być wykonywane za pomocą Dispatchera
                            {
                                image.Source = new BitmapImage(new Uri(att.Value, UriKind.Absolute)); //pobranie obrazka i wyświetlenie na image'u (podanie URLa w argumentach BitmapImage spowoduje jego automatyczne pobranie)
                            });
                    }
                    else (sender as WebClient).DownloadStringAsync(new Uri("http://kwejk.pl/losuj/")); //inaczej ma pobierać jeszcze raz
                }
            }
            catch
            {
                Deployment.Current.Dispatcher.BeginInvoke(() => //Dispatcher też wymagany
                {
                    MessageBox.Show("Sprawdź połączenie z internetem. Rodzaj błędu: " + e.Error.Message + ".", "Błąd!", MessageBoxButton.OK); //pokazuje sobie errora - w zmienną e funkcja wrzuca informacje o błędzie
                });
            }
            Deployment.Current.Dispatcher.BeginInvoke(() => //znowu
            {
                indicator.IsVisible = false; //ukrywa indicatora
            });
        }

        private void ApplicationBarIconButton_Click(object sender, System.EventArgs e)
        {
            WebClient client = new WebClient(); //tworzenie nowego klienta (za każdym razem tworzy nowego, żeby nie doszło do sytuacji, że jeden klient musi być używany jednocześnie przez dwie różne funkcje
            Deployment.Current.Dispatcher.BeginInvoke(() => //znowu
                 {
                     indicator.IsVisible = true; //pokazuje się indicator
                 });
            client.DownloadStringCompleted += client_DownloadStringCompleted; //to samo
            client.DownloadStringAsync(new Uri("http://kwejk.pl/losuj/")); //to samo
        }


        #region Obsługa powiększania obrazka (również pochodzi z SDK)

        private Point Center;
        private double InitialScale;

        private void GestureListener_PinchStarted(object sender, PinchStartedGestureEventArgs e)
        {
            // Store the initial rotation angle and scaling
            InitialScale = ImageTransformation.ScaleX;
            // Calculate the center for the zooming
            Point firstTouch = e.GetPosition(image, 0);
            Point secondTouch = e.GetPosition(image, 1);

            Center = new Point(firstTouch.X + (secondTouch.X - firstTouch.X) / 2.0, firstTouch.Y + (secondTouch.Y - firstTouch.Y) / 2.0);
        }

        private void OnPinchDelta(object sender, PinchGestureEventArgs e)
        {
            // If its less that the original  size or more than 4x then don’t apply
            if (InitialScale * e.DistanceRatio > 4 || (InitialScale != 1 && e.DistanceRatio == 1) || InitialScale * e.DistanceRatio < 1)
                return;

            // If its original size then center it back
            if (e.DistanceRatio <= 1.08)
            {
                ImageTransformation.CenterY = 0;
                ImageTransformation.CenterY = 0;
                ImageTransformation.TranslateX = 0;
                ImageTransformation.TranslateY = 0;
            }

            ImageTransformation.CenterX = Center.X;
            ImageTransformation.CenterY = Center.Y;

            // Update the rotation and scaling
            if (this.Orientation == PageOrientation.Landscape)
            {
                // When in landscape we need to zoom faster, if not it looks choppy
                ImageTransformation.ScaleX = InitialScale * (1 + (e.DistanceRatio - 1) * 2);
            }
            else
            {
                ImageTransformation.ScaleX = InitialScale * e.DistanceRatio;
            }
            ImageTransformation.ScaleY = ImageTransformation.ScaleX;
        }

        private void Image_DragDelta(object sender, DragDeltaGestureEventArgs e)
        {
            // if is not touch enabled or the scale is different than 1 then don’t allow moving
            if (ImageTransformation.ScaleX <= 1.1)
                return;

            double centerX = ImageTransformation.CenterX;
            double centerY = ImageTransformation.CenterY;
            double translateX = ImageTransformation.TranslateX;
            double translateY = ImageTransformation.TranslateY;
            double scale = ImageTransformation.ScaleX;
            double width = image.ActualWidth;
            double height = image.ActualHeight;

            // Verify limits to not allow the image to get out of area
            if (centerX - scale * centerX + translateX + e.HorizontalChange < 0 && centerX + scale * (width - centerX) + translateX + e.HorizontalChange > width)
            {
                ImageTransformation.TranslateX += e.HorizontalChange;
            }

            if (centerY - scale * centerY + translateY + e.VerticalChange < 0 && centerY + scale * (height - centerY) + translateY + e.VerticalChange > height)
            {
                ImageTransformation.TranslateY += e.VerticalChange;
            }

            return;
        }

        #endregion
    }
}