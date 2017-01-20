
namespace ProcessKiller
{
    using GoogleAnalyticsTracker.Mvc4;

    internal class AppTracker
    {
        private const string AccountName = "UA-90446002-1";
        private const string TrackingDomain = "小猪多开器 V1.3.0";
        

        public static async void TrackPageView(string pageTitle, string pageUrl)
        {
            using (var tracker = new Tracker(AccountName, TrackingDomain))
            {
                await tracker.TrackPageViewAsync(pageTitle, pageUrl);
            }
        }

        public static async void TrackEvent(string category, string action)
        {
            using (var tracker = new Tracker(AccountName, TrackingDomain))
            {
                await tracker.TrackEventAsync(category, action);
            }
        }
    }
}
