/*Auto create
Don't Edit it*/

using System;
using System.Collections.Generic;

namespace F8Framework.F8ExcelDataClass
{
	[Serializable]
	public class BulletChainItem
	{
	public int id;
	public int baseAtt;
	public int lastCount;
	public int copyCount;
	}
	
	[Serializable]
	public class BulletChain
	{
		public Dictionary<int, BulletChainItem> Dict = new Dictionary<int, BulletChainItem>();
	}
}
