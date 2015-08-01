using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Linq;
using System.ServiceProcess;
using System.Text;
using NWNMasterServer.libs;

namespace NWNMasterServer
{
    public partial class nwnmastersrv : ServiceBase
    {
        NWNMasterServer server;

        public nwnmastersrv()
        {
            InitializeComponent();
        }

        protected override void OnStart(string[] args)
        {
            server = new NWNMasterServer();
            server.Start();
        }

        protected override void OnStop()
        {
        }
    }
}
