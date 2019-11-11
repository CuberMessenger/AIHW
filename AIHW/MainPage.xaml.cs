using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;

namespace AIHW {
    public sealed partial class MainPage : Page {
        public MainPage() {
            this.InitializeComponent();
            MainPageNavigationViewFrame.NavigateToType(typeof(TSPLSPage), null, null);
        }

        private void MainPageNavigationView_ItemInvoked(NavigationView sender, NavigationViewItemInvokedEventArgs args) {
            FrameNavigationOptions navOptions = new FrameNavigationOptions();
            navOptions.TransitionInfoOverride = args.RecommendedNavigationTransitionInfo;
            if (sender.PaneDisplayMode == NavigationViewPaneDisplayMode.Top) {
                navOptions.IsNavigationStackEnabled = false;
            }
            var itemContainer = args.InvokedItemContainer.Name;
            Type pageType = null;
            if (itemContainer == "TSPLS") {
                pageType = typeof(TSPLSPage);
            }
            else if (itemContainer == "TSPLSSA") {
                pageType = typeof(TSPLSSAPage);
            }
            else if (itemContainer == "BPNN") {
                pageType = typeof(BPNNPage);
            }
            else if (itemContainer == "CNN") {
                pageType = typeof(CNNPage);
            }
            MainPageNavigationViewFrame.NavigateToType(pageType, null, navOptions);

        }
    }
}
