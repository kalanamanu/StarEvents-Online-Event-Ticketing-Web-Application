using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using System.Web.Mvc.Html;

namespace StarEvents.Utilities
{
    public static class HtmlExtensions
    {
        public static string IsActive(this HtmlHelper html, string controller, string action)
        {
            // Get current route values
            var routeData = html.ViewContext.RouteData;
            var routeAction = (string)routeData.Values["action"];
            var routeController = (string)routeData.Values["controller"];

            // Check if the current Controller/Action matches the link's Controller/Action
            bool isCurrent =
                string.Equals(routeAction, action, System.StringComparison.OrdinalIgnoreCase) &&
                string.Equals(routeController, controller, System.StringComparison.OrdinalIgnoreCase);

            // Return "active" class if it matches, otherwise return an empty string
            return isCurrent ? "active" : string.Empty;
        }

        // Optional: A simpler method to check only the Controller (useful for grouping links)
        public static string IsControllerActive(this HtmlHelper html, string controller)
        {
            var routeData = html.ViewContext.RouteData;
            var routeController = (string)routeData.Values["controller"];

            // Check if the current Controller matches the link's Controller
            bool isCurrentController =
                string.Equals(routeController, controller, System.StringComparison.OrdinalIgnoreCase);

            return isCurrentController ? "active" : string.Empty;
        }
    }
}