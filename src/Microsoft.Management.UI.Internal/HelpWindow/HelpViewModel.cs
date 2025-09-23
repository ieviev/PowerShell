// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Globalization;
using System.Management.Automation;
using System.Windows.Documents;

namespace Microsoft.Management.UI.Internal
{
    
    internal class HelpViewModel : INotifyPropertyChanged
    {
        
        private readonly HelpParagraphBuilder helpBuilder;

        
        private readonly ParagraphSearcher searcher;

        
        private readonly string helpTitle;

        
        private double zoom = 100;

        
        private string findText;

        
        private string matchesLabel;

        
        /// <param name="psObj">Object containing help.</param>
        /// <param name="documentParagraph">Paragraph in which help text is built/searched.</param>
        internal HelpViewModel(PSObject psObj, Paragraph documentParagraph)
        {
            Debug.Assert(psObj != null, "ensured by caller");
            Debug.Assert(documentParagraph != null, "ensured by caller");

            this.helpBuilder = new HelpParagraphBuilder(documentParagraph, psObj);
            this.helpBuilder.BuildParagraph();
            this.searcher = new ParagraphSearcher();
            this.helpBuilder.PropertyChanged += this.HelpBuilder_PropertyChanged;
            this.helpTitle = string.Format(
                CultureInfo.CurrentCulture,
                HelpWindowResources.HelpTitleFormat,
                HelpParagraphBuilder.GetPropertyString(psObj, "name"));
        }

        #region INotifyPropertyChanged Members
        
        public event PropertyChangedEventHandler PropertyChanged;
        #endregion

        
        public double Zoom
        {
            get
            {
                return this.zoom;
            }

            set
            {
                this.zoom = value;
                this.OnNotifyPropertyChanged("Zoom");
                this.OnNotifyPropertyChanged("ZoomLabel");
                this.OnNotifyPropertyChanged("ZoomLevel");
            }
        }

        
        public double ZoomLevel
        {
            get
            {
                return this.zoom / 100.0;
            }
        }

        
        public string ZoomLabel
        {
            get
            {
                return string.Format(CultureInfo.CurrentCulture, HelpWindowResources.ZoomLabelTextFormat, this.zoom);
            }
        }

        
        public string FindText
        {
            get
            {
                return this.findText;
            }

            set
            {
                this.findText = value;
                this.Search();
                this.SetMatchesLabel();
            }
        }

        
        public string HelpTitle
        {
            get
            {
                return this.helpTitle;
            }
        }

        
        public string MatchesLabel
        {
            get
            {
                return this.matchesLabel;
            }

            set
            {
                this.matchesLabel = value;
                this.OnNotifyPropertyChanged("MatchesLabel");
            }
        }

        
        public bool CanGoToNextOrPrevious
        {
            get
            {
                return this.HelpBuilder.HighlightCount != 0;
            }
        }

        
        internal ParagraphSearcher Searcher
        {
            get { return this.searcher; }
        }

        
        internal HelpParagraphBuilder HelpBuilder
        {
            get { return this.helpBuilder; }
        }

        
        internal void Search()
        {
            this.HelpBuilder.HighlightAllInstancesOf(this.findText, HelpWindowSettings.Default.HelpSearchMatchCase, HelpWindowSettings.Default.HelpSearchWholeWord);
            this.searcher.ResetSearch();
        }

        
        internal void ZoomIn()
        {
            if (this.Zoom + HelpWindow.ZoomInterval <= HelpWindow.MaximumZoom)
            {
                this.Zoom += HelpWindow.ZoomInterval;
            }
        }

        
        internal void ZoomOut()
        {
            if (this.Zoom - HelpWindow.ZoomInterval >= HelpWindow.MinimumZoom)
            {
                this.Zoom -= HelpWindow.ZoomInterval;
            }
        }

        
        /// <param name="sender">Event sender.</param>
        /// <param name="e">Event arguments.</param>
        private void HelpBuilder_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == "HighlightCount")
            {
                this.SetMatchesLabel();
                this.OnNotifyPropertyChanged("CanGoToNextOrPrevious");
            }
        }

        
        private void SetMatchesLabel()
        {
            if (this.findText == null || this.findText.Trim().Length == 0)
            {
                this.MatchesLabel = string.Empty;
            }
            else
            {
                if (this.HelpBuilder.HighlightCount == 0)
                {
                    this.MatchesLabel = HelpWindowResources.NoMatches;
                }
                else
                {
                    if (this.HelpBuilder.HighlightCount == 1)
                    {
                        this.MatchesLabel = HelpWindowResources.OneMatch;
                    }
                    else
                    {
                        this.MatchesLabel = string.Format(
                            CultureInfo.CurrentCulture,
                            HelpWindowResources.SomeMatchesFormat,
                            this.HelpBuilder.HighlightCount);
                    }
                }
            }
        }

        
        /// <param name="propertyName">Property name.</param>
        private void OnNotifyPropertyChanged(string propertyName)
        {
            PropertyChangedEventHandler handler = this.PropertyChanged;
            if (handler != null)
            {
                handler(this, new PropertyChangedEventArgs(propertyName));
            }
        }
    }
}
