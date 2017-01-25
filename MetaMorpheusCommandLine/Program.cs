﻿using InternalLogicEngineLayer;
using InternalLogicTaskLayer;
using IO.MzML;
using MassSpectrometry;
using Spectra;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace MetaMorpheusCommandLine
{
    internal class Program
    {

        #region Private Fields

        private const string elementsLocation = @"elements.dat";
        private const string unimodLocation = @"unimod_tables.xml";
        private const string uniprotLocation = @"ptmlist.txt";
        private static bool inProgress;

        #endregion Private Fields

        #region Private Methods

        private static void Main(string[] args)
        {
            if (MyEngine.MetaMorpheusVersion.Equals("1.0.0.0"))
                Console.WriteLine("Not a release version");
            else
                Console.WriteLine(MyEngine.MetaMorpheusVersion);

            if (args == null || args.Length == 0)
            {
                Console.WriteLine("Usage:");
                Console.WriteLine("\tmodern: runs the modern search engine");
                Console.WriteLine("\tsearch: runs the search task");
                return;
            }

            MyEngine.FinishedSingleEngineHandler += MyEngine_finishedSingleEngineHandler;
            MyEngine.OutLabelStatusHandler += MyEngine_outLabelStatusHandler;
            MyEngine.OutProgressHandler += MyEngine_outProgressHandler;
            MyEngine.StartingSingleEngineHander += MyEngine_startingSingleEngineHander;

            MyTaskEngine.FinishedSingleTaskHandler += MyTaskEngine_finishedSingleTaskHandler;
            MyTaskEngine.FinishedWritingFileHandler += MyTaskEngine_finishedWritingFileHandler;
            MyTaskEngine.StartingSingleTaskHander += MyTaskEngine_startingSingleTaskHander;
            
        }
        
        private static void MyTaskEngine_startingSingleTaskHander(object sender, SingleTaskEventArgs e)
        {
            if (inProgress)
                Console.WriteLine();
            inProgress = false;
            Console.WriteLine("Starting task:");
            Console.WriteLine(e.TheTask);
        }

        private static void MyTaskEngine_finishedWritingFileHandler(object sender, SingleFileEventArgs e)
        {
            if (inProgress)
                Console.WriteLine();
            inProgress = false;
            Console.WriteLine("Finished writing file: " + e.writtenFile);
        }

        private static void MyTaskEngine_finishedSingleTaskHandler(object sender, SingleTaskEventArgs e)
        {
            if (inProgress)
                Console.WriteLine();
            inProgress = false;
            Console.WriteLine("Finished task: " + e.TheTask.GetType().Name);
        }

        private static void MyEngine_startingSingleEngineHander(object sender, SingleEngineEventArgs e)
        {
            if (inProgress)
                Console.WriteLine();
            inProgress = false;
            Console.WriteLine("Starting engine:" + e.myEngine.GetType().Name);
        }

        private static void MyEngine_outProgressHandler(object sender, ProgressEventArgs e)
        {
            Console.Write(e.new_progress + " ");
            inProgress = true;
        }

        private static void MyEngine_outLabelStatusHandler(object sender, StringEventArgs e)
        {
            if (inProgress)
                Console.WriteLine();
            inProgress = false;
            Console.WriteLine("Status: " + e.s);
        }

        private static void MyEngine_finishedSingleEngineHandler(object sender, SingleEngineFinishedEventArgs e)
        {
            if (inProgress)
                Console.WriteLine();
            inProgress = false;
            Console.WriteLine("Finished engine: ");
            Console.WriteLine(e);
        }

        #endregion Private Methods

    }
}