﻿using System;
using System.Threading.Tasks;
using TcMenu.CoreSdk.Protocol;
using TcMenuCoreMaui.BaseSerial;
using TcMenu.CoreSdk.Util;
using TcMenuCoreMaui.AppShell;
using TcMenuCoreMaui.Services;

namespace embedCONTROL.Services
{
    /// <summary>
    /// A platform independent way of getting something done on the UI thread
    /// </summary>
    public interface UiThreadMashaller
    {
        Task OnUiThread(Action work);
    }

    public class ApplicationContext
    {
        private static object _contextLock = new object();
        private static ApplicationContext _theInstance;

        private volatile ISerialPortFactory _serialPortFactory;
        public UiThreadMashaller ThreadMarshaller {get;}

        public PrefsAppSettings AppSettings { get; }
        public ISerialPortFactory SerialPortFactory => _serialPortFactory;

        public bool IsSerialAvailable => _serialPortFactory != null;

        public SystemClock Clock => new SystemClock();

        public static ApplicationContext Instance
        {
            get
            {
                lock (_contextLock)
                {
                    return _theInstance;
                }
            }
        }

        public LibraryVersion Version { get; }

        public ApplicationContext(UiThreadMashaller marshaller, LibraryVersion version)
        {
            var configDir = Path.Combine(FileSystem.AppDataDirectory, "embedControl");
            System.IO.Directory.CreateDirectory(configDir);
            Version = version;

            ThreadMarshaller = marshaller;

            AppSettings = new PrefsAppSettings();
            AppSettings.Load(configDir);

            /*var persistor = new XmlMenuConnectionPersister(configDir);
            DataStore = new ConnectionDataStore(persistor);*/

            _theInstance = this;
        }


        public void SetSerialFactory(ISerialPortFactory factory)
        {
            _serialPortFactory = factory;
        }
    }
}