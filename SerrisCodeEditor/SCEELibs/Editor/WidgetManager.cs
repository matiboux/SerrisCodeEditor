﻿using GalaSoft.MvvmLight.Messaging;
using SCEELibs.Editor.Notifications;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Foundation;
using Windows.Foundation.Metadata;

namespace SCEELibs.Editor
{
    [AllowForWeb]
    public sealed class WidgetManager
    {
        string currentID;
        public WidgetManager(string ID)
        {
            currentID = ID;
        }

        public void enableButton(string element_name, bool enabled)
        => Messenger.Default.Send(new ToolbarNotification { id = currentID, uiElementName = element_name, propertie = ToolbarProperties.ButtonEnabled, content = enabled, answerNotification = false });

        public bool isButtonEnabled(string element_name)
        {
            bool notif_received = false, result = false;

            Messenger.Default.Register<ToolbarNotification>(this, (notification_toolbar) =>
            {
                if (notification_toolbar.id == currentID && notification_toolbar.uiElementName == element_name && notification_toolbar.propertie == ToolbarProperties.IsButtonEnabled && notification_toolbar.answerNotification)
                {
                    result = (bool)notification_toolbar.content;
                    notif_received = true;
                }
            });

            Messenger.Default.Send(new ToolbarNotification { id = currentID, uiElementName = element_name, propertie = ToolbarProperties.IsButtonEnabled, answerNotification = false });

            while (!notif_received) ;
            return result;
        }

        public void setTextBoxContent(string element_name, string text)
        => Messenger.Default.Send(new ToolbarNotification { id = currentID, uiElementName = element_name, propertie = ToolbarProperties.SetTextBoxContent, content = text, answerNotification = false });

        public string getTextBoxContent(string element_name)
        {
            string result = ""; bool notif_received = false; string guid = Guid.NewGuid().ToString();

            Messenger.Default.Register<ToolbarNotification>(this, (notification_toolbar) =>
            {
                if (notification_toolbar.id == currentID && notification_toolbar.guid == guid && notification_toolbar.uiElementName == element_name && notification_toolbar.propertie == ToolbarProperties.GetTextBoxContent && notification_toolbar.answerNotification)
                {
                    result = (string)notification_toolbar.content;
                    notif_received = true;
                }
            });

            Messenger.Default.Send(new ToolbarNotification { id = currentID, guid = guid, uiElementName = element_name, propertie = ToolbarProperties.GetTextBoxContent, answerNotification = false });

            while (!notif_received) ;
            return result;
        }

        public void openFlyout(string element_name, string html_page)
        => Messenger.Default.Send(new ToolbarNotification { id = currentID, uiElementName = element_name, propertie = ToolbarProperties.OpenFlyout, content = html_page, answerNotification = false });

        public void closeFlyout(string element_name)
        => Messenger.Default.Send(new ToolbarNotification { id = currentID, uiElementName = element_name, propertie = ToolbarProperties.CloseFlyout, content = null, answerNotification = false });

    }
}
