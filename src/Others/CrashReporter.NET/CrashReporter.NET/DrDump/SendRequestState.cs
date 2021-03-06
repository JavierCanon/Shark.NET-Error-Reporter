﻿using System;
using System.Globalization;
using System.Net;
using CrashReporterDotNET.com.drdump;

namespace CrashReporterDotNET.DrDump
{
    internal class SendRequestState
    {
        public AnonymousData AnonymousData { get; set; }

        public PrivateData PrivateData { get; set; }

        public SendAnonymousReportCompletedEventArgs SendAnonymousReportResult { get; set; }

        private static ExceptionInfo ConvertToExceptionInfo(Exception e, bool anonymous)
        {
            if (e == null)
                return null;
            return new ExceptionInfo
            {
                Type = e.GetType().ToString(),
                HResult = System.Runtime.InteropServices.Marshal.GetHRForException(e),
                StackTrace = e.StackTrace,
                Source = e.Source,
                Message = anonymous ? null : e.Message,
                InnerException = ConvertToExceptionInfo(e.InnerException, anonymous)
            };
        }

        private static System.Net.NetworkInformation.PhysicalAddress GetMacAddress()
        {
            IPAddress localAddress;
            using (var googleDns = new System.Net.Sockets.UdpClient("8.8.8.8", 53))
            {
                localAddress = ((IPEndPoint) googleDns.Client.LocalEndPoint).Address;
            }

            foreach (var netInterface in System.Net.NetworkInformation.NetworkInterface.GetAllNetworkInterfaces())
            {
                foreach (var addr in netInterface.GetIPProperties().UnicastAddresses)
                {
                    if (addr.Address.Equals(localAddress))
                        return netInterface.GetPhysicalAddress();
                }
            }

            return null;
        }

        private int GetAnonymousMachineID()
        {
            System.Net.NetworkInformation.PhysicalAddress mac = GetMacAddress();
            return mac != null
                ? BitConverter.ToInt32(System.Security.Cryptography.MD5.Create().ComputeHash(mac.GetAddressBytes()), 0)
                : 0;
        }

        internal DetailedExceptionDescription GetDetailedExceptionDescription()
        {
            return new DetailedExceptionDescription
            {
                Exception = GetExceptionDescription(false),
                DeveloperMessage = PrivateData.DeveloperMessage,
                UserDescription = PrivateData.UserMessage,
                UserEmail = PrivateData.UserEmail,
                PngScreenShot = PrivateData.Screenshot
            };
        }
        
        internal ExceptionDescription GetExceptionDescription(bool anonymous)
        {
            var oldCulture = System.Threading.Thread.CurrentThread.CurrentCulture;
            var oldUICulture = System.Threading.Thread.CurrentThread.CurrentUICulture;
            System.Threading.Thread.CurrentThread.CurrentCulture = CultureInfo.InvariantCulture;
            System.Threading.Thread.CurrentThread.CurrentUICulture = CultureInfo.InvariantCulture;

            var osVersion = Environment.OSVersion;
            var os = $"os={osVersion.Platform};v={HelperMethods.GetOSVersion()};spname={osVersion.ServicePack}";

            var exceptionDescription = new ExceptionDescription
            {
                ClrVersion = Environment.Version.ToString(),
                OS = os,
                CrashDate = DateTime.UtcNow,
                PCID = GetAnonymousMachineID(),
                Exception = ConvertToExceptionInfo(AnonymousData.Exception, anonymous),
                ExceptionString = anonymous ? null : AnonymousData.Exception.ToString(),
            };

            System.Threading.Thread.CurrentThread.CurrentCulture = oldCulture;
            System.Threading.Thread.CurrentThread.CurrentUICulture = oldUICulture;

            return exceptionDescription;
        }

        internal Application GetApplication()
        {
            var mainAssembly = System.Reflection.Assembly.GetEntryAssembly();

            string moduleName = mainAssembly.GetName().Name;

            var attributes = mainAssembly.GetCustomAttributes(typeof(System.Reflection.AssemblyCompanyAttribute), true);
            string appCompany = attributes.Length > 0
                ? ((System.Reflection.AssemblyCompanyAttribute) attributes[0]).Company
                : AnonymousData.ToEmail;

            var attributes2 = mainAssembly.GetCustomAttributes(typeof(System.Reflection.AssemblyTitleAttribute), true);
            string appTitle = attributes2.Length > 0
                ? ((System.Reflection.AssemblyTitleAttribute) attributes2[0]).Title
                : moduleName;

            var appVersion = mainAssembly.GetName().Version;

            return new Application
            {
                ApplicationGUID = AnonymousData.ApplicationID?.ToString("D"),
                AppName = appTitle,
                CompanyName = appCompany,
                Email = AnonymousData.ToEmail,
                V1 = (ushort) appVersion.Major,
                V2 = (ushort) appVersion.Minor,
                V3 = (ushort) appVersion.Build,
                V4 = (ushort) appVersion.Revision,
                MainModule = moduleName
            };
        }

        internal static ClientLib GetClientLib()
        {
            var clientVersion = typeof(CrashReport).Assembly.GetName().Version;
            return new ClientLib
            {
                V1 = (ushort) clientVersion.Major,
                V2 = (ushort) clientVersion.Minor,
                V3 = (ushort) clientVersion.Build,
                V4 = (ushort) clientVersion.Revision
            };
        }
    }
}