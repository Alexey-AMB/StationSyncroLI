using System;
using System.Collections;
using System.IO;
using System.Xml.Serialization;
using SSCE;

namespace T6.FileCopy
{
	/// <summary>
	/// 
	/// </summary>
	public class TerminalStatus
	{
		public stIni stCurTerm = new stIni();
		public string sTermName = "НЕТ ДАННЫХ";
		public bool bTerminalRead_Done = false;

		public TerminalStatus()
		{
		}

		public bool LoadIniFile(string sFullIniFileName)
		{			
			FileStream fs2 = null;
			try
			{
				fs2 = new FileStream(sFullIniFileName, FileMode.Open);
				XmlSerializer sr2 = new XmlSerializer(typeof(stIni));
				stCurTerm = (stIni)sr2.Deserialize(fs2);
				fs2.Close();
				ClassFileCopy.WriteProtocol(" LoadIniFile:OK", 2);
				sTermName = stCurTerm.sNameTerminal;
				return true;
			}
			catch (Exception ex)
			{
				if (fs2 != null) fs2.Close();
				ClassFileCopy.WriteProtocol(" LoadIniFile:ERROR - " + ex.Message, 1);
				return false;
			}		
		}

		public bool SaveIniFile(string sFullIniFileName)
		{
			FileStream fs = null;
			try
			{				
					fs = new FileStream(sFullIniFileName, FileMode.Create);
					XmlSerializer sr = new XmlSerializer(typeof(stIni));
					sr.Serialize(fs, stCurTerm);
					fs.Close();
					ClassFileCopy.WriteProtocol(" SaveIniFile:OK", 2);
					return true;				
			}
			catch (Exception ex)
			{
				if (fs != null) fs.Close();
				ClassFileCopy.WriteProtocol(" SaveIniFile:ERROR - " + ex.Message, 1);
				return false;
			}
		}

		public bool ChangeLastUpdatesByTypeBase(string sTypeBase, string sLastUpdatesName, string sLastUpdatesDate)
		{
			int i = 0;
			bool bTmp = false;
			try
			{
				if (stCurTerm.arBases != null)
				{
					foreach (stOneBase ob in stCurTerm.arBases)
					{
						if (ob.sTypeBase == sTypeBase)
						{
							bTmp = true;
							break;
						}
						i++;
					}
				}
				if (bTmp)
				{
					stCurTerm.arBases[i].sNameLastUpdates = sLastUpdatesName;
					stCurTerm.arBases[i].sDateLastUpdates = sLastUpdatesDate;
				}
			}
			catch
			{
				bTmp = false;
			}
			return bTmp;
		}

		public string GetTypeBaseByNameFileBase(string sNameFileBase)
		{
			string sRetStr = null;
			try
			{
				foreach (stOneBase ob in stCurTerm.arBases)
				{
					if (ob.sNameFileBase == sNameFileBase) sRetStr = ob.sTypeBase;
				}
			}
			catch { }
			return sRetStr;
		}

	}
}
