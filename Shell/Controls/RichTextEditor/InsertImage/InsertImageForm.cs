using Sitecore.Configuration;
using Sitecore.Data;
using Sitecore.Data.Items;
using Sitecore.Diagnostics;
using Sitecore.Globalization;
using Sitecore.IO;
using Sitecore.Resources;
using Sitecore.Resources.Media;
using Sitecore.Shell;
using Sitecore.Shell.Framework;
using Sitecore.StringExtensions;
using Sitecore.Text;
using Sitecore.Web;
using Sitecore.Web.UI.HtmlControls;
using Sitecore.Web.UI.Pages;
using Sitecore.Web.UI.Sheer;
using Sitecore.Web.UI.WebControls;
using System;
using System.Collections.Specialized;
using System.Drawing;
using System.IO;
using System.Text;
using System.Web;
using System.Web.UI;
using System.Xml.Linq;

namespace Sitecore.Foundation.Extension.MediaType.Shell.Controls.RichTextEditor.InsertImage
{
    public class InsertImageForm : DialogForm
    {
        /// <summary>
        /// The data context.
        /// 
        /// </summary>
        protected DataContext DataContext;

        /// <summary>
        /// The filename.
        /// 
        /// </summary>
        protected Edit Filename;

        /// <summary>
        /// The list view.
        /// 
        /// </summary>
        protected Scrollbox Listview;

        /// <summary>
        /// The tree view.
        /// 
        /// </summary>
        protected TreeviewEx Treeview;

        /// <summary>
        /// The upload button.
        /// 
        /// </summary>
        protected Button Upload;

        /// <summary>
        /// The edit button.
        /// 
        /// </summary>
        protected Button EditButton;

        /// <summary>
        /// Gets the content language.
        /// 
        /// </summary>
        /// 
        /// <value>
        /// The content language.
        /// </value>
        protected Language ContentLanguage
        {
            get
            {
                Language result;

                if (!Language.TryParse(WebUtil.GetQueryString("la"), out result))
                {
                    result = Context.ContentLanguage;
                }

                return result;
            }
        }

        /// <summary>
        /// Gets or sets the mode.
        /// 
        /// </summary>
        /// 
        /// <value>
        /// The mode.
        /// </value>
        protected string Mode
        {
            get { return Assert.ResultNotNull(StringUtil.GetString(ServerProperties["Mode"], "shell")); }
            set
            {
                Assert.ArgumentNotNull(value, "value");
                ServerProperties["Mode"] = value;
            }
        }

        /// <summary>
        /// Gets or sets a value indicating whether [upload button disabled].
        /// 
        /// </summary>
        /// 
        /// <value>
        /// <c>true</c> if [upload button disabled]; otherwise, <c>false</c>.
        /// 
        /// </value>
        protected bool UploadButtonDisabled
        {
            get
            {
                bool result;

                if (bool.TryParse(StringUtil.GetString(ServerProperties["UploadButtonDisabled"], "false"),
                    out result))
                {
                    return result;
                }

                return false;
            }
            set
            {
                if (value == UploadButtonDisabled)
                {
                    return;
                }

                ServerProperties["UploadButtonDisabled"] = value;
                string str =
                    "var uploadButton = document.getElementById(\"{0}\");\r\n                                    if (uploadButton){{\r\n                                        uploadButton.disabled = {1};\r\n                                    }}".FormatWith(Upload.UniqueID, value.ToString().ToLowerInvariant());

                if (Context.Page.Page.IsPostBack)
                {
                    SheerResponse.Eval(str);
                }
                else
                {
                    Context.Page.Page.ClientScript.RegisterStartupScript(GetType(), "UploadButtonModification", str,
                        true);
                }
            }
        }

        /// <summary>
        /// Handles the message.
        /// </summary>
        /// <param name="message">The message.</param>
        public override void HandleMessage(Message message)
        {
            Assert.ArgumentNotNull(message, "message");

            if (message.Name == "item:load")
            {
                LoadItem(message);
            }
            else
            {
                Dispatcher.Dispatch(message, GetCurrentItem(message));
                base.HandleMessage(message);
            }
        }

        /// <summary>
        /// Edits this instance.
        /// </summary>
        protected void Edit()
        {
            Item selectionItem = Treeview.GetSelectionItem();

            if (selectionItem == null || selectionItem.TemplateID == TemplateIDs.MediaFolder ||
                selectionItem.TemplateID == TemplateIDs.MainSection)
            {
                SheerResponse.Alert("Select a media item.");
            }
            else
            {
                UrlString urlString = new UrlString("/sitecore/shell/Applications/Content Manager/default.aspx");
                urlString["fo"] = selectionItem.ID.ToString();
                urlString["mo"] = "popup";
                urlString["wb"] = "0";
                urlString["pager"] = "0";
                urlString[State.Client.UsesBrowserWindowsQueryParameterName] = "1";
                Context.ClientPage.ClientResponse.ShowModalDialog(urlString.ToString(),
                    string.Equals(Context.Language.Name, "ja-jp", StringComparison.InvariantCultureIgnoreCase)
                        ? "1115"
                        : "955", "560");
            }
        }

        /// <summary>
        /// Handles the list view click event.
        /// </summary>
        /// <param name="id">The id.</param>
        protected void Listview_Click(string id)
        {
            Assert.ArgumentNotNullOrEmpty(id, "id");
            Item obj = Sitecore.Client.ContentDatabase.GetItem(id, ContentLanguage);

            if (obj == null)
            {
                return;
            }

            SelectItem(obj);
        }

        /// <summary>
        /// Handles a click on the Cancel button.
        /// 
        /// </summary>
        /// <param name="sender">The sender.</param><param name="args">The arguments.</param>
        /// <remarks>
        /// When the user clicksCancel, the dialog is closed by calling
        ///             the <see cref="M:Sitecore.Web.UI.Sheer.ClientResponse.CloseWindow">CloseWindow</see> method.
        /// </remarks>
        protected override void OnCancel(object sender, EventArgs args)
        {
            Assert.ArgumentNotNull(sender, "sender");
            Assert.ArgumentNotNull(args, "args");
            if (Mode == "webedit")
            {
                base.OnCancel(sender, args);
            }
            else
            {
                SheerResponse.Eval("scCancel()");
            }
        }

        /// <summary>
        /// Raises the load event.
        /// </summary>
        /// <param name="e">The <see cref="T:System.EventArgs"/> instance containing the event data.</param>
        /// <remarks>
        /// This method notifies the server control that it should perform actions common to each HTTP
        ///             request for the page it is associated with, such as setting up a database query. At this
        ///             stage in the page lifecycle, server controls in the hierarchy are created and initialized,
        ///             view state is restored, and form controls reflect client-side data. Use the IsPostBack
        ///             property to determine whether the page is being loaded in response to a client postback,
        ///             or if it is being loaded and accessed for the first time.
        /// </remarks>
        protected override void OnLoad(EventArgs e)
        {
            Assert.ArgumentNotNull(e, "e");
            base.OnLoad(e);
            if (Context.ClientPage.IsEvent)
            {
                return;
            }

            Mode = WebUtil.GetQueryString("mo");
            DataContext.GetFromQueryString();
            string queryString = WebUtil.GetQueryString("fo");

            if (ShortID.IsShortID(queryString))
            {
                DataContext.Folder = ShortID.Parse(queryString).ToID().ToString();
            }

            Context.ClientPage.ServerProperties["mode"] = WebUtil.GetQueryString("mo");

            if (!string.IsNullOrEmpty(WebUtil.GetQueryString("databasename")))
            {
                DataContext.Parameters = "databasename=" + WebUtil.GetQueryString("databasename");
            }

            Item folder = DataContext.GetFolder();
            Assert.IsNotNull(folder, "Folder not found");
            SelectItem(folder);

            Upload.Click = "media:upload(edit=" + (Settings.Media.OpenContentEditorAfterUpload ? "1" : "0") +
                           ",load=1)";
            Upload.ToolTip = Translate.Text("Upload a new media file to the Media Library");
            EditButton.ToolTip = Translate.Text("Edit the media item in the Content Editor.");
        }

        /// <summary>
        /// Handles a click on the OK button.
        /// 
        /// </summary>
        /// <param name="sender">The sender.</param><param name="args">The arguments.</param>
        /// <remarks>
        /// When the user clicks OK, the dialog is closed by calling
        ///             the <see cref="M:Sitecore.Web.UI.Sheer.ClientResponse.CloseWindow">CloseWindow</see> method.
        /// 
        /// </remarks>
        protected override void OnOK(object sender, EventArgs args)
        {
            Assert.ArgumentNotNull(sender, "sender");
            Assert.ArgumentNotNull(args, "args");

            string str = Filename.Value;
            if (str.Length == 0)
            {
                SheerResponse.Alert("Select a media item.");
            }
            else
            {
                Item root = DataContext.GetRoot();
                Item rootItem = root?.Database.GetRootItem();

                if (rootItem != null && root.ID != rootItem.ID)
                {
                    str = FileUtil.MakePath(root.Paths.Path, str, '/');
                }

                MediaItem mediaItem = DataContext.GetItem(str, ContentLanguage, Sitecore.Data.Version.Latest);
                if (mediaItem == null)
                {
                    SheerResponse.Alert("The media item could not be found.");
                }
                else if (!(MediaManager.GetMedia(MediaUri.Parse(mediaItem)) is ImageMedia || mediaItem.MimeType == "image/svg+xml"))
                {
                    SheerResponse.Alert("The selected item is not an image. Select an image to continue.");
                }
                else
                {
                    MediaUrlOptions shellOptions = MediaUrlOptions.GetShellOptions();
                    shellOptions.Language = ContentLanguage;
                    string text = !string.IsNullOrEmpty(HttpContext.Current.Request.Form["AlternateText"])
                        ? HttpContext.Current.Request.Form["AlternateText"]
                        : mediaItem.Alt;
                    
                    if (mediaItem.MimeType == "image/svg+xml")
                    {
                        string svgImage = GetSVGImage(mediaItem, shellOptions);
                        
                        if (Mode == "webedit")
                        {
                            SheerResponse.SetDialogValue(StringUtil.EscapeJavascriptString(svgImage));
                            base.OnOK(sender, args);
                        }
                        else
                        {
                            SheerResponse.Eval("scClose(" + StringUtil.EscapeJavascriptString(svgImage) + ")");
                        }
                    }
                    else
                    {
                        Tag image = new Tag("img");
                        SetDimensions(mediaItem, shellOptions, image);
                        image.Add("Src", MediaManager.GetMediaUrl(mediaItem, shellOptions));
                        image.Add("Alt", StringUtil.EscapeQuote(text));
                        image.Add("_languageInserted", "true");

                        if (Mode == "webedit")
                        {
                            SheerResponse.SetDialogValue(StringUtil.EscapeJavascriptString(image.ToString()));
                            base.OnOK(sender, args);
                        }
                        else
                        {
                            SheerResponse.Eval("scClose(" + StringUtil.EscapeJavascriptString(image.ToString()) + ")");
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Selects the tree node.
        /// </summary>
        protected void SelectTreeNode()
        {
            Item selectionItem = Treeview.GetSelectionItem(ContentLanguage, Sitecore.Data.Version.Latest);

            if (selectionItem == null)
            {
                return;
            }

            SelectItem(selectionItem, false);
        }

        /// <summary>
        /// Renders the empty.
        /// </summary>
        /// <param name="output">The output.</param>
        private static void RenderEmpty(HtmlTextWriter output)
        {
            Assert.ArgumentNotNull(output, "output");
            output.Write("<table width=\"100%\" border=\"0\"><tr><td align=\"center\">");
            output.Write("<div style=\"padding:8px\">");
            output.Write(Translate.Text("This folder is empty."));
            output.Write("</div>");
            output.Write("</td></tr></table>");
        }

        /// <summary>
        /// Renders the list view item.
        /// </summary>
        /// <param name="output">The output.</param><param name="item">The child.</param>
        private static void RenderListviewItem(HtmlTextWriter output, Item item)
        {
            Assert.ArgumentNotNull(output, "output");
            Assert.ArgumentNotNull(item, "item");
            MediaItem mediaItem = item;
            output.Write(
                "<a href=\"#\" class=\"scTile\" onclick=\"javascript:return scForm.postEvent(this,event,'Listview_Click(&quot;" +
                item.ID + "&quot;)')\" >");
            output.Write("<div class=\"scTileImage\">");

            if (item.TemplateID == TemplateIDs.Folder || item.TemplateID == TemplateIDs.TemplateFolder || item.TemplateID == TemplateIDs.MediaFolder)
            {
                new ImageBuilder
                {
                    Src = item.Appearance.Icon,
                    Width = 48,
                    Height = 48,
                    Margin = "24px 24px 24px 24px"
                }.Render(output);
            }
            else
            {
                MediaUrlOptions shellOptions = MediaUrlOptions.GetShellOptions();
                shellOptions.AllowStretch = false;
                shellOptions.BackgroundColor = Color.White;
                shellOptions.Language = item.Language;
                shellOptions.Thumbnail = true;
                shellOptions.UseDefaultIcon = true;
                shellOptions.Width = 96;
                shellOptions.Height = 96;
                string mediaUrl = MediaManager.GetMediaUrl(mediaItem, shellOptions);
                output.Write("<img src=\"" + mediaUrl + "\" class=\"scTileImageImage\" border=\"0\" alt=\"\" />");
            }

            output.Write("</div>");
            output.Write("<div class=\"scTileHeader\">");
            output.Write(item.DisplayName);
            output.Write("</div>");
            output.Write("</a>");
        }

        /// <summary>
        /// Renders the preview.
        /// </summary>
        /// <param name="output">The output.</param><param name="item">The item.</param>
        private static void RenderPreview(HtmlTextWriter output, Item item)
        {
            Assert.ArgumentNotNull(output, "output");
            Assert.ArgumentNotNull(item, "item");
            MediaItem mediaItem = item;
            MediaUrlOptions shellOptions = MediaUrlOptions.GetShellOptions();
            shellOptions.AllowStretch = false;
            shellOptions.BackgroundColor = Color.White;
            shellOptions.Language = item.Language;
            shellOptions.Thumbnail = true;
            shellOptions.UseDefaultIcon = true;
            shellOptions.Width = 192;
            shellOptions.Height = 192;
            string mediaUrl = MediaManager.GetMediaUrl(mediaItem, shellOptions);
            output.Write("<table width=\"100%\" height=\"100%\" border=\"0\" cellpadding=\"0\" cellspacing=\"0\">");
            output.Write("<tr><td align=\"center\" height=\"100%\">");
            output.Write("<div class=\"scPreview\">");
            output.Write("<img src=\"" + mediaUrl + "\" class=\"scPreviewImage\" border=\"0\" alt=\"\" />");
            output.Write("</div>");
            output.Write("<div class=\"scPreviewHeader\">");
            output.Write(item.DisplayName);
            output.Write("</div>");
            output.Write("</td></tr>");
            if (!(MediaManager.GetMedia(MediaUri.Parse(mediaItem)) is ImageMedia || mediaItem.MimeType == "image/svg+xml"))
            {
                output.Write("</table>");
            }
            else
            {
                output.Write("<tr><td class=\"scProperties\">");
                output.Write("<table border=\"0\" class=\"scFormTable\" cellpadding=\"2\" cellspacing=\"0\">");
                output.Write("<col align=\"right\" />");
                output.Write("<col align=\"left\" />");
                output.Write("<tr><td>");
                output.Write(Translate.Text("Alternate text:"));
                output.Write("</td><td>");
                output.Write("<input type=\"text\" id=\"AlternateText\" value=\"{0}\" />",
                    HttpUtility.HtmlEncode(mediaItem.Alt));
                output.Write("</td></tr>");
                output.Write("<tr><td>");
                output.Write(Translate.Text("Width:"));
                output.Write("</td><td>");
                output.Write("<input type=\"text\" id=\"Width\" value=\"{0}\" />",
                    HttpUtility.HtmlEncode(mediaItem.InnerItem["Width"]));
                output.Write("</td></tr>");
                output.Write("<tr><td>");
                output.Write(Translate.Text("Height:"));
                output.Write("</td><td>");
                output.Write("<input type=\"text\" id=\"Height\" value=\"{0}\" />",
                    HttpUtility.HtmlEncode(mediaItem.InnerItem["Height"]));
                output.Write("</td></tr>");
                output.Write("</table>");
                output.Write("</td></tr>");
                output.Write("</table>");
                SheerResponse.Eval("scAspectPreserver.reload();");
            }
        }

        /// <summary>
        /// Gets the current item.
        /// 
        /// </summary>
        /// <param name="message">The message.</param>
        /// <returns>
        /// The current item.
        /// 
        /// </returns>
        private Item GetCurrentItem(Message message)
        {
            Assert.ArgumentNotNull(message, "message");
            string index = message["id"];
            Language language = DataContext.Language;
            Item folder = DataContext.GetFolder();

            if (folder != null)
            {
                language = folder.Language;
            }

            if (!string.IsNullOrEmpty(index))
            {
                return Sitecore.Client.ContentDatabase.Items[index, language];
            }

            return folder;
        }

        /// <summary>
        /// Loads the item.
        /// </summary>
        /// <param name="message">The message.</param>
        private void LoadItem(Message message)
        {
            Assert.ArgumentNotNull(message, "message");
            Language language = DataContext.Language;
            Item folder = DataContext.GetFolder();

            if (folder != null)
            {
                language = folder.Language;
            }

            Item obj = Sitecore.Client.ContentDatabase.GetItem(ID.Parse(message["id"]), language);

            if (obj == null)
            {
                return;
            }

            SelectItem(obj);
        }

        /// <summary>
        /// Updates the preview.
        /// 
        /// </summary>
        /// <param name="item">The item.</param><param name="expand">If set to <c>true</c> then item will show it descendants.</param>
        private void SelectItem(Item item, bool expand = true)
        {
            Assert.ArgumentNotNull(item, "item");
            UploadButtonDisabled = !item.Access.CanCreate();
            Filename.Value = ShortenPath(item.Paths.Path);
            DataContext.SetFolder(item.Uri);

            if (expand)
            {
                Treeview.SetSelectedItem(item);
            }

            HtmlTextWriter output = new HtmlTextWriter(new StringWriter());
            if (item.TemplateID == TemplateIDs.Folder || item.TemplateID == TemplateIDs.MediaFolder ||
                item.TemplateID == TemplateIDs.MainSection)
            {
                foreach (Item obj in item.Children)
                {
                    if (obj.Appearance.Hidden)
                    {
                        if (Context.User.IsAdministrator && UserOptions.View.ShowHiddenItems)
                        {
                            RenderListviewItem(output, obj);
                        }
                    }
                    else
                    {
                        RenderListviewItem(output, obj);
                    }
                }
            }
            else
            {
                RenderPreview(output, item);
            }

            string str = output.InnerWriter.ToString();

            if (string.IsNullOrEmpty(str))
            {
                RenderEmpty(output);
                str = output.InnerWriter.ToString();
            }
            Listview.InnerHtml = str;
        }

        /// <summary>
        /// Gets the dimensions.
        /// </summary>
        /// <param name="item">The item.</param><param name="options">The options.</param><param name="image">The image.</param>
        private void SetDimensions(MediaItem item, MediaUrlOptions options, Tag image)
        {
            Assert.ArgumentNotNull(item, "item");
            Assert.ArgumentNotNull(options, "options");
            Assert.ArgumentNotNull(image, "image");
            NameValueCollection form = HttpContext.Current.Request.Form;
            if (!string.IsNullOrEmpty(form["Width"]) && form["Width"] != item.InnerItem["Width"] &&
                form["Height"] != item.InnerItem["Height"])
            {
                int result1;

                if (int.TryParse(form["Width"], out result1))
                {
                    options.Width = result1;
                    image.Add("width", result1.ToString());
                }

                int result2;

                if (!int.TryParse(form["Height"], out result2))
                {
                    return;
                }
                options.Height = result2;
                image.Add("height", result2.ToString());
            }
            else
            {
                image.Add("width", item.InnerItem["Width"]);
                image.Add("height", item.InnerItem["Height"]);
            }
        }

        private string GetSVGImage(MediaItem item, MediaUrlOptions options)
        {
            Assert.ArgumentNotNull(item, "item");
            Assert.ArgumentNotNull(options, "options");

            string result;

            using (StreamReader reader = new StreamReader(item.GetMediaStream(), Encoding.UTF8))
            {
                result = reader.ReadToEnd();
            }

            NameValueCollection form = HttpContext.Current.Request.Form;

            XDocument svg = XDocument.Parse(result);

            if (svg.Document?.Root != null)
            {
                int width;

                if (int.TryParse(form["Width"], out width))
                {
                    svg.Document.Root.SetAttributeValue("width", width);
                }

                int height;

                if (int.TryParse(form["Height"], out height))
                {
                    svg.Document.Root.SetAttributeValue("height", height);
                }

                result = svg.ToString();
            }
            
            return result;
        }

        /// <summary>
        /// Shortens the path.
        /// </summary>
        /// <param name="path">The path.</param>
        /// <returns>
        /// The shorten path.
        /// </returns>
        private string ShortenPath(string path)
        {
            Assert.ArgumentNotNull(path, "path");
            Item root = DataContext.GetRoot();

            Item rootItem = root?.Database.GetRootItem();

            if (rootItem != null && root.ID != rootItem.ID)
            {
                string path1 = root.Paths.Path;
                if (path.StartsWith(path1, StringComparison.InvariantCulture))
                {
                    path = StringUtil.Mid(path, path1.Length);
                }
            }
            return Assert.ResultNotNull(path);
        }
    }
}