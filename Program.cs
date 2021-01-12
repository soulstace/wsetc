﻿using Microsoft.Win32;
using System;
using System.Diagnostics;
using System.IO;
using System.Management;
using System.ServiceProcess;

namespace wsetc
{
    class Program
    {
        static void Main(string[] args)
        {
            if (args.Length > 0 && args[0].Contains(".reg"))
                WriteFile(args[0]);
            else
            {
                Console.WriteLine("");
                Console.WriteLine("Windows Services Export Tool v1.0");
                Console.WriteLine("Copyright (C) 2021 soulstace");
                Console.WriteLine("github.com/soulstace/wsetc");
                Console.WriteLine("");
                Console.WriteLine("Back up your current Windows services configuration in an easy-to-read/modify/restore .reg format");
                Console.WriteLine("");
                Console.WriteLine("Usage: wsetc services.reg");
            }
        }

        static void WriteFile(string path)
        {
            string mypath = Environment.ExpandEnvironmentVariables(path);
            if (File.Exists(mypath))
            {
                Console.WriteLine(mypath + " already exists. Use a different filename.");
                return;
            }
            Console.WriteLine("Exporting services into file " + mypath);

            FileVersionInfo krnl = FileVersionInfo.GetVersionInfo(Path.Combine(Environment.SystemDirectory, "ntoskrnl.exe"));
            ServiceController[] services = ServiceController.GetServices();
            using (StreamWriter sw = File.CreateText(mypath))
            {
                sw.WriteLine("Windows Registry Editor Version 5.00");
                sw.WriteLine("");
                sw.WriteLine("; Generated by Windows Services Export Tool");
                sw.WriteLine("; {0:U} UTC", DateTime.UtcNow);
                sw.WriteLine("; Author: soulstace");
                sw.WriteLine("");
                //sw.WriteLine("; OS Version: " + Environment.OSVersion.VersionString);
                sw.WriteLine("; OS/Kernel Version: " + krnl.FileVersion); /* new method for windows 10 */
                sw.WriteLine("; Total services: " + services.Length);
                sw.WriteLine("");
                sw.WriteLine("; DWORD values and their meanings;");
                sw.WriteLine("; 2 = Automatic");
                sw.WriteLine("; 3 = Manual");
                sw.WriteLine("; 4 = Disabled");
                sw.WriteLine("");

                foreach (ServiceController sc in services)
                {
                    if (!string.IsNullOrEmpty(sc.ServiceName))
                    {
                        RegistryKey rkSvc = Registry.LocalMachine.OpenSubKey(@"SYSTEM\CurrentControlSet\Services\" + sc.ServiceName,
                            RegistryKeyPermissionCheck.ReadSubTree, System.Security.AccessControl.RegistryRights.ReadKey);
                        int intStart = (int)rkSvc.GetValue("Start", 0);

                        // service description is obtained using the Extended ServiceController class by Mohamed Sharaf
                        string mgpth = "Win32_Service.Name='" + sc.ServiceName + "'";
                        ManagementObject mgobj = new ManagementObject(new ManagementPath(mgpth));
                        string svcdesc = (mgobj["Description"] != null) ? mgobj["Description"].ToString() : "";
                        mgobj.Dispose();

                        sw.WriteLine("; " + sc.DisplayName);
                        sw.WriteLine("; " + svcdesc);
                        sw.WriteLine(@"[HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\Services\" + sc.ServiceName + "]");

                        if (!intStart.Equals(0))
                        {
                            sw.WriteLine("\"Start\"=dword:" + intStart);

                            if ((int)rkSvc.GetValue("DelayedAutoStart", 0) == 1)
                                sw.WriteLine("\"DelayedAutoStart\"=dword:1");
                        }
                        else
                            sw.WriteLine("; Start is 0 or unknown");
                        sw.WriteLine("");
                        rkSvc.Close();
                    }
                }
            }
        }
    }
}
