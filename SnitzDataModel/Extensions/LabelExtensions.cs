using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Web;
using System.Web.Mvc;
using System.Web.Mvc.Html;
using SnitzCore.Extensions;
using LangResources.Utility;
using System.Globalization;
using SnitzConfig;
using SnitzCore.Utility;


namespace SnitzDataModel.Extensions
{
    public static partial class Extensions
    {
        /// <summary>
        /// Renders action link with confirmation popup
        /// </summary>
        /// <param name="helper"></param>
        /// <param name="title"></param>
        /// <param name="action"></param>
        /// <param name="controller"></param>
        /// <param name="routeValues"></param>
        /// <param name="tagclass"></param>
        /// <param name="btn">Render as button flag</param>
        /// <returns></returns>
        public static MvcHtmlString ActionLinkConfirm(this HtmlHelper helper, string title, string action, string controller, object routeValues, string tagclass, bool btn = false)
        {
            UrlHelper urlHelper = new UrlHelper(HttpContext.Current.Request.RequestContext);

            var tagid = Guid.NewGuid().ToString();

            var tag = new TagBuilder("a");
            tag.MergeAttribute("href", "javascript:;");

            tag.MergeAttribute("data-title", title);
            tag.MergeAttribute("data-toggle", "tooltip");
            tag.MergeAttribute("rel", "nofollow");
            tag.MergeAttribute("onclick", "BootstrapDialog.confirm({title: '" + title + "', message: '" + ResourceManager.GetLocalisedString("Are_you_sure", "labels") + "', callback: function(ok) {if(ok) executeCallback('#" + tagid + "'); } })");
            if (btn)
            {
                tag.MergeAttribute("class", tagclass);
                tag.InnerHtml = title;
            }
            else
            {
                tag.InnerHtml = "<i class='" + tagclass + "'></i>";
            }


            var hiddentag = new TagBuilder("a");
            hiddentag.AddCssClass("hidden");
            hiddentag.AddCssClass("confirmed-link");
            hiddentag.MergeAttribute("rel", "nofollow");
            hiddentag.MergeAttribute("id", tagid);
            hiddentag.MergeAttribute("href", urlHelper.Action(action, controller, routeValues));

            var sb = new StringBuilder();
            sb.AppendLine(hiddentag.ToString(TagRenderMode.Normal));
            sb.AppendLine(tag.ToString(TagRenderMode.Normal));

            return new MvcHtmlString(sb.ToString());
        }
        private static readonly SelectListItem[] SingleEmptyItem = new[] { new SelectListItem { Text = "", Value = "" } };

        /// <summary>
        /// Generate dropdown list from Enum
        /// </summary>
        /// <typeparam name="TModel"></typeparam>
        /// <typeparam name="TEnum"></typeparam>
        /// <param name="htmlHelper"></param>
        /// <param name="expression"></param>
        /// <param name="htmlAttributes"></param>
        /// <param name="lastvisit"></param>
        /// <returns></returns>
        public static MvcHtmlString EnumDropDownListFor<TModel, TEnum>(this HtmlHelper<TModel> htmlHelper, Expression<Func<TModel, TEnum>> expression, object htmlAttributes, string lastvisit)
        {

            ModelMetadata metadata = ModelMetadata.FromLambdaExpression(expression, htmlHelper.ViewData);
            Type enumType = SnitzCore.Extensions.Extensions.GetNonNullableModelType(metadata);
            var values = Enum.GetValues(enumType).Cast<TEnum>();

            IEnumerable<SelectListItem> items = from value in values
                                                select new SelectListItem
                                                {
                                                    Text = LangResources.Utility.ResourceManager.GetLocalisedString(value.GetType().Name + "_" + value).Replace("[[LASTVISIT]]", lastvisit),
                                                    Value = value.ToString(),
                                                    Selected = value.Equals(metadata.Model)
                                                };

            // If the enum is nullable, add an 'empty' item to the collection
            if (metadata.IsNullableValueType)
                items = SingleEmptyItem.Concat(items);

            return htmlHelper.DropDownListFor(expression, items, htmlAttributes);
        }

        /// <summary>
        /// Only renders the html if the condition is true
        /// </summary>
        /// <param name="s"></param>
        /// <param name="condition"></param>
        /// <returns></returns>
        public static MvcHtmlString If(this MvcHtmlString s, bool condition)
        {
            return condition ? s : MvcHtmlString.Empty;
        }

        public static MvcHtmlString Label(this HtmlHelper html, string expression, string id = "", bool generatedId = false)
        {
            return LabelHelper(html,
                ModelMetadata.FromStringExpression(expression, html.ViewData),
                expression, DisplayOptions.None, "", id, generatedId);
        }

        [SuppressMessage("Microsoft.Design", "CA1006:DoNotNestGenericTypesInMemberSignatures", Justification = "This is an appropriate nesting of generic types")]
        public static MvcHtmlString LabelFor<TModel, TValue>(this HtmlHelper<TModel> html, Expression<Func<TModel, TValue>> expression, DisplayOptions displayOptions, string extraText = "", string id = "", bool generatedId = false)
        {
            return LabelHelper(html,
                ModelMetadata.FromLambdaExpression(expression, html.ViewData),
                ExpressionHelper.GetExpressionText(expression), displayOptions, extraText, id, generatedId);
        }

        private static MvcHtmlString LabelHelper(HtmlHelper html, ModelMetadata metadata, string htmlFieldName, DisplayOptions displayOptions, string extraText, string id, bool generatedId)
        {
            string labelText = metadata.DisplayName ?? metadata.PropertyName ?? htmlFieldName.Split('.').Last();
            if (String.IsNullOrEmpty(labelText))
            {
                return MvcHtmlString.Empty;
            }
            var sb = new StringBuilder();
            if (displayOptions == DisplayOptions.Humanize || displayOptions == DisplayOptions.HumanizeAndColon)
            {
                sb.Append(labelText.Titleize());
            }
            else
            {
                sb.Append(labelText);
            }
            sb.Append(extraText);
            if (displayOptions == DisplayOptions.HumanizeAndColon && labelText.Substring(labelText.Length - 1) != ":")
            {
                sb.Append(":");
            }

            var tag = new TagBuilder("label");
            if (!String.IsNullOrWhiteSpace(id))
            {
                tag.Attributes.Add("id", id);
            }
            else if (generatedId)
            {
                tag.Attributes.Add("id", html.ViewContext.ViewData.TemplateInfo.GetFullHtmlFieldId(htmlFieldName) + "_Label");
            }

            tag.Attributes.Add("for", html.ViewContext.ViewData.TemplateInfo.GetFullHtmlFieldId(htmlFieldName));
            tag.SetInnerText(sb.ToString());

            return MvcHtmlString.Create(tag.ToString(TagRenderMode.Normal));
        }
        public static string ToFormattedString(this DateTime date, bool showtime = true)
        {
            if (date == DateTime.MinValue)
            {
                return "";
            }
            //return ToFormattedString(date, showtime);
            CultureInfo ci = SessionData.Get<CultureInfo>("Culture");
            var dateformat = ResourceManager.GetLocalisedString("dateLong", "dateFormat");
            var result = "";
            if (ci.TwoLetterISOLanguageName.ToLower() == "fa")
            {
                if (showtime)
                {
                    dateformat = dateformat + " " + Config.TimeStr;
                }
                PersianCalendar persianCal = new PersianCalendar();
                CalendarUtility persianUtil = new CalendarUtility(persianCal, dateformat);
                CultureInfo ic = CultureInfo.CreateSpecificCulture("fa-IR");

                result = persianUtil.DisplayDate(date, ic);

            }
            else
            {
                if (showtime)
                {
                    dateformat = dateformat + " " + Config.TimeStr;
                }
                result = date.ToString(dateformat, ci);
            }
            //var dateformat = Config.DateStr + (showtime ? " " + Config.TimeStr : "");
            return result;
        }


    }

    public enum DisplayOptions
    {
        Humanize,
        HumanizeAndColon,
        None
    }
}