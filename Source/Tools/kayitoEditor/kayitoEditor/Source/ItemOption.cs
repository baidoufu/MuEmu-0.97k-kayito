using System;
using System.Collections.Generic;
using System.Windows.Forms;
using static kayito_Editor.Source.ReadScript;

namespace kayito_Editor.Source
{
	public class ItemOption
	{
		public struct ITEM_OPTION_INFO
		{
			public int Index;
			public int OptionIndex;
			public int OptionValue;
			public int ItemMinIndex;
			public int ItemMaxIndex;
			public int ItemSkillOption;
			public int ItemLuckOption;
			public int ItemAddOption;
			public int ItemExceOption;
		};

		public static IDictionary<int, string> m_ItemOptions = new Dictionary<int, string>()
		{
			{ -1, "No Info" },
			{ 0, "Enabled: +HP // Disabled: +Dmg" },
			{ 60, "Additional Dmg +X" },
			{ 61, "Additional Wizardry Dmg +X" },
			{ 62, "Additional defense rate +X%" },
			{ 63, "Additional defense +X" },
			{ 64, "Luck (critical damage rate +5%)" },
			{ 65, "Automatic HP recovery X%" },
			{ 66, "最大生命值 +4%" },
			{ 67, "最大魔法值 +4%" },
			{ 68, "伤害减少 +4%" },
			{ 69, "伤害反射 +5%" },
			{ 70, "防御成功率 +10%" },
			{ 71, "杀死怪物时所获金增加 +30%" },
			{ 72, "卓越攻击几率增加 +10%" },
			{ 73, "攻击力增加 +等级/20" },
			{ 74, "攻击力增加 +X%" },
			{ 75, "魔法攻击力增加 +等级/20" },
			{ 76, "魔法攻击力增加 +X%" },
			{ 77, "攻击(魔法)速度增加 +X" },
			{ 78, "杀死怪物时所获生命值增加\n +生命值/8" },
			{ 79, "杀死怪物时所获魔法值增加\n +魔法值/8" },
			{ 80, "生命值增加 +X" },
			{ 81, "魔法值增加 +X" },
			{ 82, "X% 概率攻击时无视对方防御" },
			{ 83, "技能值最大值 +X" },
			{ 84, "Absorb X% additional damage" },
		};

		public static IDictionary<int, List<ITEM_OPTION_INFO>> m_ItemOptionInfo = new Dictionary<int, List<ITEM_OPTION_INFO>>();

		public static void ReadItemOptionTxt()
		{
			string path = ".\\Data\\ItemOption.txt";

			ReadScript lpReadScript = new ReadScript();

			if (!lpReadScript.SetBuffer(path))
			{
				MessageBox.Show(lpReadScript.GetLastError(), "ReadItemOptionTxt");

				return;
			}

			ItemOption.m_ItemOptionInfo.Clear();

			try
			{
				eTokenResult token;

				while (true)
				{
					token = lpReadScript.GetToken();

					if (token == eTokenResult.TOKEN_END || token == eTokenResult.TOKEN_END_SECTION)
					{
						break;
					}

					ITEM_OPTION_INFO info = new ITEM_OPTION_INFO();

					info.Index = lpReadScript.GetNumber();

					info.OptionIndex = lpReadScript.GetAsNumber();

					info.OptionValue = lpReadScript.GetAsNumber();

					info.ItemMinIndex = ItemManager.GET_ITEM(lpReadScript.GetAsNumber(), lpReadScript.GetAsNumber());

					info.ItemMaxIndex = ItemManager.GET_ITEM(lpReadScript.GetAsNumber(), lpReadScript.GetAsNumber());

					info.ItemSkillOption = lpReadScript.GetAsNumber();

					info.ItemLuckOption = lpReadScript.GetAsNumber();

					info.ItemAddOption = lpReadScript.GetAsNumber();

					info.ItemExceOption = lpReadScript.GetAsNumber();

					if (ItemOption.m_ItemOptionInfo.ContainsKey(info.Index))
					{
						ItemOption.m_ItemOptionInfo[info.Index].Add(info);
					}
					else
					{
						ItemOption.m_ItemOptionInfo.Add(info.Index, new List<ITEM_OPTION_INFO>() { info });
					}
				}
			}
			catch
			{
				MessageBox.Show(lpReadScript.GetLastError(), "ReadItemOptionTxt");

				Environment.Exit(0);
			}
		}

		public static bool GetItemOption(int index, int ItemIndex, out string value)
		{
			bool result = false;

			value = m_ItemOptions[-1];

			List<ITEM_OPTION_INFO> listInfo;

			if (index == 8 && (ItemIndex >= ItemManager.GET_ITEM(12, 3) && ItemIndex <= ItemManager.GET_ITEM(12, 6)))
			{
				result = true;

				value = m_ItemOptions[0];
			}
			else if (m_ItemOptionInfo.TryGetValue(index, out listInfo))
			{
				foreach (ITEM_OPTION_INFO itemInfo in listInfo)
				{
					if (itemInfo.ItemMinIndex != -1 && itemInfo.ItemMinIndex > ItemIndex)
					{
						continue;
					}

					if (itemInfo.ItemMaxIndex != -1 && itemInfo.ItemMaxIndex < ItemIndex)
					{
						continue;
					}

					result = true;

					if (index == 0)
					{
						if (itemInfo.OptionIndex == 0 && ItemManager.GetItemSkill(ItemIndex) == 0)
						{
							result = false;
						}
					}
					else
					{
						value = m_ItemOptions[itemInfo.OptionIndex];
					}
				}
			}

			return result;
		}
	}
}