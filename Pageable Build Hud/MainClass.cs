using BepInEx;
using BepInEx.Configuration;
using HarmonyLib;
using Jotunn.Managers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;

namespace ch.easy.develope.ch.pageable.build.hud
{


    [BepInPlugin("ch.easy.develope.ch.pageable.build.hud", "Pageable Build Hud", "1.0.2")]
    [BepInProcess("valheim.exe")]

    public class MainClass : BaseUnityPlugin
    {
        public static Harmony harmony;
		public static int page_index = 0;
		public static float max_pages = 1;
		public static int current_cat = 0;
		public static int cat_count = 0;

		public static GameObject button_next;
		public static GameObject button_prev;
		public static GameObject txt_PageOf;

		public static PieceTable pTable;
		public static HashSet<string> l_knownRecipes;
		public static bool l_hideUnavailable = true;
		public static bool l_noPlacementCost = false;

		void Awake()
        {
            harmony = new Harmony("ch.easy.develope.ch.pageable.build.hud");
            harmony.PatchAll();
            Debug.Log("Pageable Build Hud Loaded!");
        }
	}

	[HarmonyPatch(typeof(Hud), "Awake")]
	public class Hud_Awake
	{
		[HarmonyPrefix]
		public static void Prefix(Hud __instance)
		{
			if(__instance != null)
            {
				//Debug.LogError("Page Index is: " + MainClass.page_index);

				MainClass.button_next = GUIManager.Instance.CreateButton(
					text: "Next",
					parent: __instance.m_pieceSelectionWindow.transform,
					anchorMin: new Vector2(0.5f, 0.5f),
					anchorMax: new Vector2(0.5f, 0.5f),
					position: new Vector2(+250f, -300f),
					width: 250f,
					height: 60f);

				Button button_n = MainClass.button_next.GetComponent<Button>();
				button_n.onClick.AddListener(PageNext);

				MainClass.button_prev = GUIManager.Instance.CreateButton(
						text: "Previous",
						parent: __instance.m_pieceSelectionWindow.transform,
						anchorMin: new Vector2(0.5f, 0.5f),
						anchorMax: new Vector2(0.5f, 0.5f),
						position: new Vector2(-250f, -300f),
						width: 250f,
						height: 60f);

				Button button_p = MainClass.button_prev.GetComponent<Button>();
				button_p.onClick.AddListener(PagePrev);
			}
		}

		public static void PageNext()
        {
			if(MainClass.page_index < MainClass.max_pages) MainClass.page_index++;
			if (MainClass.page_index > 0) MainClass.button_prev.SetActive(true);
			if (MainClass.page_index == MainClass.max_pages) MainClass.button_next.SetActive(false);
			//Debug.LogError("Page Index is: " + MainClass.page_index);

			MainClass.pTable.UpdateAvailable(MainClass.l_knownRecipes, Player.m_localPlayer, MainClass.l_hideUnavailable, MainClass.l_noPlacementCost);

		}
		public static void PagePrev()
        {
			if(MainClass.page_index > 0) MainClass.page_index--;
			if (MainClass.page_index == 0) MainClass.button_prev.SetActive(false);
			if (MainClass.page_index < MainClass.max_pages) MainClass.button_next.SetActive(true);
			Debug.Log("Page Index is: " + MainClass.page_index);

			MainClass.pTable.UpdateAvailable(MainClass.l_knownRecipes, Player.m_localPlayer, MainClass.l_hideUnavailable, MainClass.l_noPlacementCost);

		}
	}

	[HarmonyPatch(typeof(Player), "Update")]
	public class Player_Update
	{
		[HarmonyPrefix]
		public static void Prefix(Player __instance, ref PieceTable ___m_buildPieces, ref HashSet<string> ___m_knownRecipes)
		{
			if (___m_buildPieces != null && __instance != null)
			{
				MainClass.pTable = ___m_buildPieces;
				MainClass.l_knownRecipes = ___m_knownRecipes;
			}
		}
	}

	[HarmonyPatch(typeof(PieceTable), "UpdateAvailable")]
	public class PieceTable_UpdateAvailable
	{
		[HarmonyPrefix]
		public static bool Prefix(PieceTable __instance, ref HashSet<string> knownRecipies, ref Player player, ref bool hideUnavailable, ref bool noPlacementCost, ref List<List<Piece>> ___m_availablePieces, ref List<GameObject> ___m_pieces)
		{
			MainClass.l_noPlacementCost = noPlacementCost;
			MainClass.l_hideUnavailable = hideUnavailable;
			MainClass.cat_count = 0;

			if (MainClass.current_cat != (int)__instance.m_selectedCategory) MainClass.page_index = 0;
			MainClass.current_cat = (int)__instance.m_selectedCategory;

			//Debug.LogError("UpdateAvailable");
			//Debug.LogError("___m_availablePieces count before: " + ___m_availablePieces.Count);
			//Debug.LogError("___m_pieces count before: " + ___m_pieces.Count);

			int per_page = 13*7;

			Dictionary<int, int> skipped_dict = new Dictionary<int, int>();

			if (___m_availablePieces.Count == 0)
			{
				for (int i = 0; i < 4; i++)
				{
					___m_availablePieces.Add(new List<Piece>());
				}
			}
			foreach (List<Piece> list in ___m_availablePieces)
			{
				list.Clear();
			}
			foreach (GameObject gameObject in ___m_pieces)
			{
				Piece component = gameObject.GetComponent<Piece>();
				if (noPlacementCost || (knownRecipies.Contains(component.m_name) && component.m_enabled && (!hideUnavailable || player.HaveRequirements(component, Player.RequirementMode.CanAlmostBuild))))
				{
					if (component.m_category == Piece.PieceCategory.All)
					{
						for (int j = 0; j < 4; j++)
						{
							___m_availablePieces[j].Add(component);
						}
					}
					else
					{						
						if((int)component.m_category == MainClass.current_cat)
                        {

							int tmp_skipped = 0;
							int tmp_count = ___m_availablePieces[(int)component.m_category].Count;
							if (skipped_dict.ContainsKey((int)component.m_category))
							{
								tmp_skipped = skipped_dict[(int)component.m_category];
								if (tmp_skipped < per_page * MainClass.page_index)
								{
									tmp_skipped++;
									skipped_dict[(int)component.m_category] = tmp_skipped;
								}
							}
							else
							{
								if (tmp_skipped < per_page * MainClass.page_index)
								{
									tmp_skipped++;
									skipped_dict.Add((int)component.m_category, tmp_skipped);
								}
							}

							if (tmp_skipped >= per_page * MainClass.page_index && tmp_count <= per_page)
							{
								if (___m_availablePieces.Count() < (int)component.m_category - 1)
								{
									___m_availablePieces.Add(new List<Piece>());
								}
								else
								{
									___m_availablePieces[(int)component.m_category].Add(component);
								}
							}

							MainClass.cat_count++;
                        } 
						else
                        {
							if(___m_availablePieces.Count() < (int)component.m_category - 1)
                            {
								___m_availablePieces.Add(new List<Piece>());
							} else
                            {
								___m_availablePieces[(int)component.m_category].Add(component);
							}
						}
					}
				}
			}

			Debug.Log("Items in cat is: " + MainClass.cat_count);
			if (MainClass.cat_count > (13 * 7))
			{
				MainClass.max_pages = MainClass.cat_count / (13 * 7);
			}
			else
			{
				MainClass.max_pages = 0;
			}

			Debug.Log("Page Index is: " + MainClass.page_index);
			Debug.Log("Page max_pages is: " + MainClass.max_pages);

			if (MainClass.page_index > 0) MainClass.button_prev.SetActive(true);
			if (MainClass.page_index == MainClass.max_pages) MainClass.button_next.SetActive(false);
			if (MainClass.page_index == 0) MainClass.button_prev.SetActive(false);
			if (MainClass.page_index < MainClass.max_pages) MainClass.button_next.SetActive(true);

			return false;
		}
	}
}
