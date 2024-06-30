/*Auto create
Don't Edit it*/

using System;
using System.Collections.Generic;

namespace F8Framework.F8ExcelDataClass
{
	[Serializable]
	public class BulletGatlingItem
	{
	public int id;
	public int baseAtt;
	public int bulletCount;
	public int width;
	public int height;
	}
	
	[Serializable]
	public class BulletGatling
	{
		public Dictionary<int, BulletGatlingItem> Dict = new Dictionary<int, BulletGatlingItem>();
	}
}
