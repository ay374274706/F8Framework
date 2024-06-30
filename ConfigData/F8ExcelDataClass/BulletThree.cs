/*Auto create
Don't Edit it*/

using System;
using System.Collections.Generic;

namespace F8Framework.F8ExcelDataClass
{
	[Serializable]
	public class BulletThreeItem
	{
	public int id;
	public int baseAtt;
	public int lastCount;
	public int copyCount;
	}
	
	[Serializable]
	public class BulletThree
	{
		public Dictionary<int, BulletThreeItem> Dict = new Dictionary<int, BulletThreeItem>();
	}
}
