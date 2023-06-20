﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using embedControl.Views;
using TcMenu.CoreSdk.Commands;
using TcMenu.CoreSdk.MenuItems;
using TcMenu.CoreSdk.Protocol;
using TcMenu.CoreSdk.RemoteCore;
using TcMenu.CoreSdk.RemoteSimulator;
using TcMenu.CoreSdk.RemoteStates;
using TcMenu.CoreSdk.Serialisation;
using TcMenuCoreMaui.Controls;
using TcMenuCoreMaui.FormUi;
using TcMenuCoreMaui.Services;
using MenuItem = TcMenu.CoreSdk.MenuItems.MenuItem;

namespace embedControl.Models
{
    public class ControlToMauiColorHelper
    {
        private readonly IConditionalColoring _conditionalColoring;
        private readonly ColorComponentType _colorType;

        public RenderStatus CurrentStatus { get; set; }
        public Color Fg => _conditionalColoring.ForegroundFor(CurrentStatus, _colorType).AsXamarin();
        public Color Bg => _conditionalColoring.BackgroundFor(CurrentStatus, _colorType).AsXamarin();

        public ControlToMauiColorHelper(IConditionalColoring condColors, ColorComponentType colorType)
        {
            _colorType = colorType;
            _conditionalColoring = condColors;
        }

    }

    public class TcMenuConnectionModel : INotifyPropertyChanged
    {
        private readonly IRemoteController _remoteController;
        private readonly PrefsAppSettings _appSettings;
        private readonly MenuItem _startingPoint;
        private MenuButtonType _button1Type = MenuButtonType.NONE;
        private MenuButtonType _button2Type = MenuButtonType.NONE;

        public event PropertyChangedEventHandler PropertyChanged;

        public string ConnectionName => _remoteController?.Connector?.ConnectionName + " " + ToPrettyAuth(_remoteController?.Connector?.AuthStatus);

        public bool BackOptionNeeded => !Equals(_startingPoint, MenuTree.ROOT);


        private string ToPrettyAuth(AuthenticationStatus? auth)
        {
            return auth switch
            {
                AuthenticationStatus.NOT_STARTED => "Stopped",
                AuthenticationStatus.AWAITING_CONNECTION => "Disconnected",
                AuthenticationStatus.ESTABLISHED_CONNECTION => "Connected",
                AuthenticationStatus.SEND_JOIN => "Joining",
                AuthenticationStatus.FAILED_AUTH => "Login Failure",
                AuthenticationStatus.AUTHENTICATED => "Authenticated",
                AuthenticationStatus.BOOTSTRAPPING => "Bootstrapping",
                AuthenticationStatus.CONNECTION_READY => "Ready",
                null => "--",
                _ => throw new ArgumentOutOfRangeException(nameof(auth), auth, null)
            };
        }

        public string DetailedConnectionInfo => GetDetailedName();

        public ControlToMauiColorHelper DialogColor { get; }
        public ControlToMauiColorHelper ButtonColor { get; }
        public bool DialogOnDisplay { get; set; } = false;
        public string DialogHeader { get; set; } = "Header";
        public string DialogBuffer { get; set; } = "Buffer";
        public string Button1Text { get; set; } = "OK";
        public string Button2Text { get; set; } = "Cancel";

        public TcMenuConnectionModel(IRemoteController controller, PrefsAppSettings settings, Grid controlsGrid, MenuItem where, SubMenuNavigator navigator)
        {
            _startingPoint = where;
            _remoteController = controller;

            _appSettings = settings;
            var settingsConditional = new PrefsConditionalColoring(settings);
            DialogColor = new ControlToMauiColorHelper(settingsConditional, ColorComponentType.DIALOG);
            ButtonColor = new ControlToMauiColorHelper(settingsConditional, ColorComponentType.BUTTON);

            var editorFactory = new MauiMenuEditorFactory(_remoteController);
            var gridComponent = new TcMenuGridComponent(_remoteController, editorFactory, settings, new LoadedMenuForm(), controlsGrid, navigator);
            gridComponent.Start(_startingPoint);
            _remoteController.DialogUpdatedEvent += RemoteControllerOnDialogUpdatedEvent;
            _remoteController.Connector.ConnectionChanged += RemoteControllerStatusChanged;
        }

        public void RemoteControllerStatusChanged(AuthenticationStatus connected)
        {
            MainThread.InvokeOnMainThreadAsync(() =>
            {
                PropHasChanged(nameof(ConnectionName));
                PropHasChanged(nameof(DetailedConnectionInfo));
            });
        }

        private void RemoteControllerOnDialogUpdatedEvent(CorrelationId cor, DialogMode mode, string hdr, string msg, MenuButtonType b1, MenuButtonType b2)
        {
            MainThread.InvokeOnMainThreadAsync(() =>
            {
                DialogOnDisplay = mode != DialogMode.HIDE;
                DialogHeader = hdr;
                DialogBuffer = msg;
                Button1Text = ButtonTextOf(b1);
                Button2Text = ButtonTextOf(b2);
                _button1Type = b1;
                _button2Type = b2;
                PropHasChanged(nameof(DialogOnDisplay));
                PropHasChanged(nameof(DialogHeader));
                PropHasChanged(nameof(DialogBuffer));
                PropHasChanged(nameof(Button1Text));
                PropHasChanged(nameof(Button2Text));
            });
        }

        private void PropHasChanged(string prop)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(prop));
        }

        private string ButtonTextOf(MenuButtonType b)
        {
            return b switch
            {
                MenuButtonType.NONE => "",
                MenuButtonType.OK => "OK",
                MenuButtonType.ACCEPT => "Accept",
                MenuButtonType.CANCEL => "Cancel",
                MenuButtonType.CLOSE => "Close",
                _ => throw new ArgumentOutOfRangeException(nameof(b), b, null)
            };
        }

        public void SendDialogEvent(int btnNum)
        {
            _remoteController?.SendDialogAction(btnNum == 0 ? _button1Type : _button2Type);
        }

        private string GetDetailedName()
        {
            var connector = _remoteController?.Connector;
            if (_remoteController == null || connector == null) return "No connector";
            var remote = _remoteController.Connector.RemoteInfo;

            return $"{connector.ConnectionName}({ToPrettyAuth(connector.AuthStatus)}) - {remote.Version} {remote.Platform} S/N {remote.SerialNumber} Type {remote.Uuid}";
        }

    }

}