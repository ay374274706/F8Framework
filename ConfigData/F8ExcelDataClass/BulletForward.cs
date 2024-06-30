/*Auto create
Don't Edit it*/

using System;
using System.Collections.Generic;

namespace F8Framework.F8ExcelDataClass
{
	[Serializable]
	public class BulletForwardItem
	{
	public int id;
	public int lastCount;
	public float scale;
	public int minAtt;
	public int maxAtt;
	}
	
	[Serializable]
	public class BulletForward
	{
		public Dictionary<int, BulletForwardItem> Dict = new Dictionary<int, BulletForwardItem>();
	}
}
