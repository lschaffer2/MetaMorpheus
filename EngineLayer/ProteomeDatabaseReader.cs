﻿using Chemistry;
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Xml;

namespace EngineLayer
{
    public static class ProteomeDatabaseReader
    {

        #region Private Fields

        private static readonly Dictionary<string, ModificationType> modificationTypeCodes;
        private static readonly Dictionary<string, char> aminoAcidCodes;

        #endregion Private Fields

        #region Public Constructors

        static ProteomeDatabaseReader()
        {
            modificationTypeCodes = new Dictionary<string, ModificationType>();
            modificationTypeCodes.Add("N-terminal.", ModificationType.ProteinNTerminus);
            modificationTypeCodes.Add("C-terminal.", ModificationType.ProteinCTerminus);
            modificationTypeCodes.Add("Peptide N-terminal.", ModificationType.PeptideNTerminus);
            modificationTypeCodes.Add("Peptide C-terminal.", ModificationType.PeptideCTerminus);
            modificationTypeCodes.Add("Anywhere.", ModificationType.AminoAcidResidue);
            modificationTypeCodes.Add("Protein core.", ModificationType.AminoAcidResidue);

            aminoAcidCodes = new Dictionary<string, char>();
            aminoAcidCodes.Add("Alanine", 'A');
            aminoAcidCodes.Add("Arginine", 'R');
            aminoAcidCodes.Add("Asparagine", 'N');
            aminoAcidCodes.Add("Aspartate", 'D');
            aminoAcidCodes.Add("Aspartic Acid", 'D');
            aminoAcidCodes.Add("Cysteine", 'C');
            aminoAcidCodes.Add("Glutamate", 'E');
            aminoAcidCodes.Add("Glutamic Acid", 'E');
            aminoAcidCodes.Add("Glutamine", 'Q');
            aminoAcidCodes.Add("Glycine", 'G');
            aminoAcidCodes.Add("Histidine", 'H');
            aminoAcidCodes.Add("Isoleucine", 'I');
            aminoAcidCodes.Add("Leucine", 'L');
            aminoAcidCodes.Add("Lysine", 'K');
            aminoAcidCodes.Add("Methionine", 'M');
            aminoAcidCodes.Add("Phenylalanine", 'F');
            aminoAcidCodes.Add("Proline", 'P');
            aminoAcidCodes.Add("Serine", 'S');
            aminoAcidCodes.Add("Threonine", 'T');
            aminoAcidCodes.Add("Tryptophan", 'W');
            aminoAcidCodes.Add("Tyrosine", 'Y');
            aminoAcidCodes.Add("Valine", 'V');
            aminoAcidCodes.Add("Any", '\0');
            aminoAcidCodes.Add("Asparagine or Aspartate", 'B');
            aminoAcidCodes.Add("Undefined", '?');
        }

        #endregion Public Constructors

        #region Public Methods

        public static HashSet<string> ReadXmlModifications(IEnumerable<string> uniProtXmlProteomeDatabaseFilepaths)
        {
            var modifications_in_database = new HashSet<string>();
            foreach (var uniProtXmlProteomeDatabaseFilepath in uniProtXmlProteomeDatabaseFilepaths)
                using (var stream = new FileStream(uniProtXmlProteomeDatabaseFilepath, FileMode.Open))
                {
                    Stream uniprotXmlFileStream = stream;
                    if (uniProtXmlProteomeDatabaseFilepath.EndsWith(".gz", StringComparison.OrdinalIgnoreCase))
                        uniprotXmlFileStream = new GZipStream(stream, CompressionMode.Decompress);
                    using (XmlReader xml = XmlReader.Create(uniprotXmlFileStream))
                        while (xml.ReadToFollowing("feature"))
                            if (xml.GetAttribute("type") == "modified residue")
                            {
                                string description = xml.GetAttribute("description");
                                if (!description.Contains("variant"))
                                {
                                    int semicolon_index = description.IndexOf(';');
                                    if (semicolon_index >= 0)
                                        description = description.Substring(0, semicolon_index);
                                    modifications_in_database.Add(description);
                                }
                            }
                }
            return modifications_in_database;
        }

        public static IEnumerable<MorpheusModification> ReadModFile(string v)
        {
            using (var modsReader = new StreamReader(Path.Combine(Path.GetDirectoryName(Environment.GetCommandLineArgs()[0]), v)))
            {
                string description = null;
                string feature_type = null;
                ModificationType modification_type = ModificationType.AminoAcidResidue;
                char amino_acid_residue = '\0';
                char prevAA = '\0';
                double pms = double.NaN;
                double fms = double.NaN;
                double oms = double.NaN;
                ChemicalFormula chemicalFormula = null;
                while (modsReader.Peek() != -1)
                {
                    string line = modsReader.ReadLine();
                    if (line.Length >= 2)
                    {
                        switch (line.Substring(0, 2))
                        {
                            case "ID":
                                description = line.Substring(5);
                                break;

                            case "FT":
                                feature_type = line.Substring(5);
                                break;

                            case "TG":
                                if (feature_type == "MOD_RES")
                                {
                                    string amino_acid = line.Substring(5);
                                    amino_acid_residue = aminoAcidCodes[char.ToUpperInvariant(amino_acid[0]) + amino_acid.Substring(1).TrimEnd('.')];
                                }
                                break;

                            case "PP":
                                if (feature_type == "MOD_RES")
                                {
                                    modification_type = modificationTypeCodes[line.Substring(5)];
                                }
                                break;

                            case "MM":
                                pms = double.Parse(line.Substring(5));
                                break;

                            case "FM":
                                fms = double.Parse(line.Substring(5));
                                break;

                            case "OM":
                                oms = double.Parse(line.Substring(5));
                                break;

                            case "PS":
                                prevAA = line[5];
                                break;

                            case "CF":
                                chemicalFormula = new ChemicalFormula(line.Substring(5).Replace(" ", string.Empty));
                                break;

                            case "//":
                                if (feature_type == "MOD_RES" && (!double.IsNaN(pms)))
                                {
                                    if (chemicalFormula == null)
                                        throw new InvalidDataException("In file" + v + " Modification " + description + " has no chemical formula");
                                    if (Math.Abs(pms - chemicalFormula.MonoisotopicMass) > 1e-3)
                                        throw new InvalidDataException("In file" + v + " Modification " + description + " mass formula mismatch");
                                    yield return new MorpheusModification(description, modification_type, amino_acid_residue, Path.GetFileNameWithoutExtension(v), prevAA, pms, double.IsNaN(fms) ? pms : fms, double.IsNaN(oms) ? pms : oms, chemicalFormula);
                                }
                                description = null;
                                feature_type = null;
                                modification_type = ModificationType.AminoAcidResidue;
                                amino_acid_residue = '\0';
                                pms = double.NaN;
                                fms = double.NaN;
                                oms = double.NaN;
                                chemicalFormula = null;
                                break;
                        }
                    }
                }
            }
        }

        #endregion Public Methods

    }
}