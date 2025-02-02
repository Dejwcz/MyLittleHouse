using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.AspNetCore.Razor.TagHelpers;
using System;

namespace MujDomecek.TagHelpers {
    // This Tag Helper targets <a> elements that have asp-controller and asp-action or asp-page attributes.
    [HtmlTargetElement("a", Attributes = "asp-controller,asp-action")]
    [HtmlTargetElement("a", Attributes = "asp-page")]
    public class ActiveNavLinkTagHelper : TagHelper {
        [ViewContext]
        [HtmlAttributeNotBound]
        public ViewContext ViewContext { get; set; }

        // For MVC – controller and action
        public string AspController { get; set; }
        public string AspAction { get; set; }

        // For Razor Pages – page
        public string AspPage { get; set; }

        // For Razor Pages – area
        public string AspArea { get; set; }

        public override void Process(TagHelperContext context, TagHelperOutput output) {
            bool isActive = false;

            if (!string.IsNullOrEmpty(AspPage)) {
                // With Razor Pages, we get the current page from the routing data under the "page" key
                var currentPage = ViewContext.RouteData.Values["page"]?.ToString();
                isActive = string.Equals(currentPage, AspPage, StringComparison.OrdinalIgnoreCase);
            }
            else {
                // For MVC, we compare the current controller and action
                var currentController = ViewContext.RouteData.Values["controller"]?.ToString();
                var currentAction = ViewContext.RouteData.Values["action"]?.ToString();
                isActive = string.Equals(currentController, AspController, StringComparison.OrdinalIgnoreCase)
                           && string.Equals(currentAction, AspAction, StringComparison.OrdinalIgnoreCase);
            }

            // If the link has an area, we also compare it
            if (!string.IsNullOrEmpty(AspArea)) {
                var currentArea = ViewContext.RouteData.Values["area"]?.ToString();
                isActive = isActive && string.Equals(currentArea, AspArea, StringComparison.OrdinalIgnoreCase);
            }

            // If the link is active, we add the "active" class
            if (isActive) {
                var classAttr = output.Attributes.ContainsName("class")
                    ? output.Attributes["class"].Value.ToString()
                    : string.Empty;
                if (!classAttr.Contains("active")) {
                    var newClassValue = string.IsNullOrWhiteSpace(classAttr)
                        ? "active"
                        : $"{classAttr} active";
                    output.Attributes.SetAttribute("class", newClassValue.Trim());
                }
            }
        }
    }
}
