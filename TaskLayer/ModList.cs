﻿
using EngineLayer;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace TaskLayer
{
    public class ModList
    {
        #region Private Fields

        private readonly List<MorpheusModification> mods;

        private string FullFileName;

        #endregion Private Fields

        #region Public Constructors

        public ModList(string fileName)
        {
            FullFileName = Path.GetFullPath(fileName);
            mods = ProteomeDatabaseReader.ReadModFile(FullFileName).ToList();
            Description = File.ReadLines(FullFileName).First();
        }

        #endregion Public Constructors

        #region Public Properties

        public string FileName
        {
            get
            {
                return Path.GetFileName(FullFileName);
            }
        }

        public int Count
        {
            get { return mods.Count; }
        }

        public string Description { get; private set; }

        public List<MorpheusModification> Mods
        {
            get
            {
                return mods;
            }
        }

        #endregion Public Properties
    }
}