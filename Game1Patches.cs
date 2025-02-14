using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using StardewValley;
using StardewValley.Tools;
using StardewValley.Quests;
using StardewValley.Locations;
using StardewValley.Objects;
using System.Collections.Generic;
using StardewValley.GameData.Objects;
using StardewValley.TerrainFeatures;

namespace WeaponsOnDisplay
{
	public class Game1Patches
	{
		private static void checkIfDialogueIsQuestion()
		{
			if (Game1.currentSpeaker != null && Game1.currentSpeaker.CurrentDialogue.Count > 0 && Game1.currentSpeaker.CurrentDialogue.Peek().isCurrentDialogueAQuestion())
			{
				Game1.questionChoices.Clear();
				Game1.isQuestion = true;
				List<NPCDialogueResponse> questions = Game1.currentSpeaker.CurrentDialogue.Peek().getNPCResponseOptions();
				for (int i = 0; i < questions.Count; i++)
				{
					Game1.questionChoices.Add(questions[i]);
				}
			}
		}

		private static bool tryToPlaceItem(GameLocation location, Item item, int x, int y)
		{
			if (item == null)
			{
				return false;
			}
			Vector2 tileLocation = new Vector2((float)(x / 64), (float)(y / 64));
			if (Utility.playerCanPlaceItemHere(location, item, x, y, Game1.player, false))
			{
				if (item is Furniture)
				{
					Game1.player.ActiveObject = null;
				}
				if (item is Object objectItem && objectItem.placementAction(location, x, y, Game1.player))
				{
					Game1.player.reduceActiveItemByOne();
				}
				else
				{
					if (item is Furniture furniture)
					{
						Game1.player.ActiveObject = furniture;
					}
					else if (item is Wallpaper)
					{
						return false;
					}
				}
				return true;
			}
			if (Utility.isPlacementForbiddenHere(location) && item != null && item.isPlaceable())
			{
				if (Game1.didPlayerJustClickAtAll(true))
				{
					Game1.showRedMessage(Game1.content.LoadString("Strings\\StringsFromCSFiles:Object.cs.13053"), true);
				}
			}
			else
			{
				if (item is Furniture furniture && Game1.didPlayerJustLeftClick(true))
				{
					switch (furniture.GetAdditionalFurniturePlacementStatus(location, x, y, Game1.player))
					{
						case 1:
							Game1.showRedMessage(Game1.content.LoadString("Strings\\StringsFromCSFiles:Furniture.cs.12629"), true);
							break;
						case 2:
							Game1.showRedMessage(Game1.content.LoadString("Strings\\StringsFromCSFiles:Furniture.cs.12632"), true);
							break;
						case 3:
							Game1.showRedMessage(Game1.content.LoadString("Strings\\StringsFromCSFiles:Furniture.cs.12633"), true);
							break;
						case 4:
							Game1.showRedMessage(Game1.content.LoadString("Strings\\StringsFromCSFiles:Furniture.cs.12632"), true);
							break;
					}
				}
			}

			if (item.Category == -19 && location.terrainFeatures.TryGetValue(tileLocation, out TerrainFeature terrainFeature))
			{
				if (terrainFeature is HoeDirt dirt)
				{
					switch (dirt.CheckApplyFertilizerRules(item.QualifiedItemId))
					{
						case HoeDirtFertilizerApplyStatus.HasThisFertilizer:
							return false;
						case HoeDirtFertilizerApplyStatus.HasAnotherFertilizer:
							if (Game1.didPlayerJustClickAtAll(true))
							{
								Game1.showRedMessage(Game1.content.LoadString("Strings\\StringsFromCSFiles:HoeDirt.cs.13916-2"), true);
							}
							return false;
						case HoeDirtFertilizerApplyStatus.CropAlreadySprouted:
							if (Game1.didPlayerJustClickAtAll(true))
							{
								Game1.showRedMessage(Game1.content.LoadString("Strings\\StringsFromCSFiles:HoeDirt.cs.13916"), true);
							}
							return false;
					}
				}
			}
			Utility.playerCanPlaceItemHere(location, item, x, y, Game1.player, true);
			return false;
		}

		public static bool pressActionButton_Prefix(KeyboardState currentKBState, MouseState currentMouseState, GamePadState currentPadState, ref bool __result)
		{
			if (Game1.IsChatting)
			{
				currentKBState = default(KeyboardState);
			}
			if (Game1.dialogueTyping)
			{
				bool consume = true;
				Game1.dialogueTyping = false;
				if (Game1.currentSpeaker != null)
				{
					Game1.currentDialogueCharacterIndex = Game1.currentSpeaker.CurrentDialogue.Peek().getCurrentDialogue().Length;
				}
				else if (Game1.currentObjectDialogue.Count > 0)
				{
					Game1.currentDialogueCharacterIndex = Game1.currentObjectDialogue.Peek().Length;
				}
				else
				{
					consume = false;
				}
				Game1.dialogueTypingInterval = 0;
				Game1.oldKBState = currentKBState;
				Game1.oldMouseState = Game1.input.GetMouseState();
				Game1.oldPadState = currentPadState;
				if (consume)
				{
					Game1.playSound("dialogueCharacterClose", null);
					__result = false;
					return false;
				}
			}
			if (Game1.dialogueUp)
			{
				if (Game1.isQuestion)
				{
					Game1.isQuestion = false;
					if (Game1.currentSpeaker != null)
					{
						if (Game1.currentSpeaker.CurrentDialogue.Peek().chooseResponse(Game1.questionChoices[Game1.currentQuestionChoice]))
						{
							Game1.currentDialogueCharacterIndex = 1;
							Game1.dialogueTyping = true;
							Game1.oldKBState = currentKBState;
							Game1.oldMouseState = Game1.input.GetMouseState();
							Game1.oldPadState = currentPadState;
							__result = false;
							return false;
						}
					}
					else
					{
						Game1.dialogueUp = false;
						if (Game1.eventUp && Game1.currentLocation.afterQuestion == null)
						{
							Game1.currentLocation.currentEvent.answerDialogue(Game1.currentLocation.lastQuestionKey, Game1.currentQuestionChoice);
							Game1.currentQuestionChoice = 0;
							Game1.oldKBState = currentKBState;
							Game1.oldMouseState = Game1.input.GetMouseState();
							Game1.oldPadState = currentPadState;
						}
						else if (Game1.currentLocation.answerDialogue(Game1.questionChoices[Game1.currentQuestionChoice]))
						{
							Game1.currentQuestionChoice = 0;
							Game1.oldKBState = currentKBState;
							Game1.oldMouseState = Game1.input.GetMouseState();
							Game1.oldPadState = currentPadState;
							__result = false;
							return false;
						}
						if (Game1.dialogueUp)
						{
							Game1.currentDialogueCharacterIndex = 1;
							Game1.dialogueTyping = true;
							Game1.oldKBState = currentKBState;
							Game1.oldMouseState = Game1.input.GetMouseState();
							Game1.oldPadState = currentPadState;
							__result = false;
							return false;
						}
					}
					Game1.currentQuestionChoice = 0;
				}
				string exitDialogue = null;
				if (Game1.currentSpeaker != null)
				{
					if (Game1.currentSpeaker.immediateSpeak)
					{
						Game1.currentSpeaker.immediateSpeak = false;
						__result = false;
						return false;
					}
					exitDialogue = ((Game1.currentSpeaker.CurrentDialogue.Count > 0) ? Game1.currentSpeaker.CurrentDialogue.Peek().exitCurrentDialogue() : null);
				}
				if (exitDialogue == null)
				{
					if (Game1.currentSpeaker != null && Game1.currentSpeaker.CurrentDialogue.Count > 0 && Game1.currentSpeaker.CurrentDialogue.Peek().isOnFinalDialogue() && Game1.currentSpeaker.CurrentDialogue.Count > 0)
					{
						Game1.currentSpeaker.CurrentDialogue.Pop();
					}
					Game1.dialogueUp = false;
					if (Game1.messagePause)
					{
						Game1.pauseTime = 500f;
					}
					if (Game1.currentObjectDialogue.Count > 0)
					{
						Game1.currentObjectDialogue.Dequeue();
					}
					Game1.currentDialogueCharacterIndex = 0;
					if (Game1.currentObjectDialogue.Count > 0)
					{
						Game1.dialogueUp = true;
						Game1.questionChoices.Clear();
						Game1.oldKBState = currentKBState;
						Game1.oldMouseState = Game1.input.GetMouseState();
						Game1.oldPadState = currentPadState;
						Game1.dialogueTyping = true;
						__result = false;
						return false;
					}
					if (Game1.currentSpeaker != null && !Game1.currentSpeaker.Name.Equals("Gunther") && !Game1.eventUp && !Game1.currentSpeaker.doingEndOfRouteAnimation.Value)
					{
						Game1.currentSpeaker.doneFacingPlayer(Game1.player);
					}
					Game1.currentSpeaker = null;
					if (!Game1.eventUp)
					{
						Game1.player.CanMove = true;
					}
					else if (Game1.currentLocation.currentEvent.CurrentCommand > 0 || Game1.currentLocation.currentEvent.specialEventVariable1)
					{
						if (!Game1.isFestival() || !Game1.currentLocation.currentEvent.canMoveAfterDialogue())
						{
							Game1.currentLocation.currentEvent.CurrentCommand++;
						}
						else
						{
							Game1.player.CanMove = true;
						}
					}
					Game1.questionChoices.Clear();
					Game1.playSound("smallSelect", null);
				}
				else
				{
					Game1.playSound("smallSelect", null);
					Game1.currentDialogueCharacterIndex = 0;
					Game1.dialogueTyping = true;
					checkIfDialogueIsQuestion();
				}
				Game1.oldKBState = currentKBState;
				Game1.oldMouseState = Game1.input.GetMouseState();
				Game1.oldPadState = currentPadState;
				__result = false;
				return false;
			}
			if (!Game1.player.UsingTool && (!Game1.eventUp || (Game1.currentLocation.currentEvent != null && Game1.currentLocation.currentEvent.playerControlSequence)) && !Game1.fadeToBlack)
			{
				if (Game1.wasMouseVisibleThisFrame && Game1.currentLocation.animals.Length > 0)
				{
					Vector2 mousePosition = new Vector2(Game1.getOldMouseX() + Game1.viewport.X, Game1.getOldMouseY() + Game1.viewport.Y);
					if (Utility.withinRadiusOfPlayer((int)mousePosition.X, (int)mousePosition.Y, 1, Game1.player))
					{
						if (Game1.currentLocation.CheckPetAnimal(mousePosition, Game1.player))
						{
							__result = true;
							return false;
						}
						if (Game1.didPlayerJustRightClick(true) && Game1.currentLocation.CheckInspectAnimal(mousePosition, Game1.player))
						{
							__result = true;
							return false;
						}
					}
				}
				Vector2 grabTile = new Vector2(Game1.getOldMouseX() + Game1.viewport.X, Game1.getOldMouseY() + Game1.viewport.Y) / 64f;
				Vector2 cursorTile = grabTile;
				bool non_directed_tile = false;
				if (!Game1.wasMouseVisibleThisFrame || Game1.mouseCursorTransparency == 0f || !Utility.tileWithinRadiusOfPlayer((int)grabTile.X, (int)grabTile.Y, 1, Game1.player))
				{
					grabTile = Game1.player.GetGrabTile();
					non_directed_tile = true;
				}

				bool was_character_at_grab_tile = false;
				if (Game1.eventUp && !Game1.isFestival())
				{
					Event currentEvent = Game1.CurrentEvent;
					if (currentEvent != null)
					{
						currentEvent.receiveActionPress((int)grabTile.X, (int)grabTile.Y);
					}
					Game1.oldKBState = currentKBState;
					Game1.oldMouseState = Game1.input.GetMouseState();
					Game1.oldPadState = currentPadState;
					__result = false;
					return false;
				}
				
				if (Game1.tryToCheckAt(grabTile, Game1.player))
				{
					__result = false;
					return false;
				}
				if (Game1.player.isRidingHorse())
				{
					Game1.player.mount.checkAction(Game1.player, Game1.player.currentLocation);
					__result = false;
					return false;
				}
				if (!Game1.player.canMove)
				{
					__result = false;
					return false;
				}
				if (!was_character_at_grab_tile && Game1.player.currentLocation.isCharacterAtTile(grabTile) != null)
				{
					was_character_at_grab_tile = true;
				}
				bool isPlacingObject = false;
				if (Game1.player.ActiveObject != null && !(Game1.player.ActiveObject is Furniture))
				{
					Item placedItem = null;
					if (Game1.player.ActiveObject != null)
					{
						placedItem = Game1.player.ActiveObject;
						if (Game1.player.ActiveObject.performUseAction(Game1.currentLocation))
						{
							Game1.player.reduceActiveItemByOne();
							Game1.oldKBState = currentKBState;
							Game1.oldMouseState = Game1.input.GetMouseState();
							Game1.oldPadState = currentPadState;
							__result = false;
							return false;
						}
					}
					else
					{
						placedItem = Game1.player.CurrentTool;
					}

					int stack = placedItem.Stack;
					Game1.isCheckingNonMousePlacement = !Game1.IsPerformingMousePlacement();
					if (non_directed_tile)
					{
						Game1.isCheckingNonMousePlacement = true;
					}
					if (Game1.isOneOfTheseKeysDown(currentKBState, Game1.options.actionButton))
					{
						Game1.isCheckingNonMousePlacement = true;
					}
					
					Vector2 valid_position = Utility.GetNearbyValidPlacementPosition(Game1.player, Game1.currentLocation, placedItem, (int)grabTile.X * 64 + 32, (int)grabTile.Y * 64 + 32);
					if (!Game1.isCheckingNonMousePlacement && placedItem is Wallpaper && tryToPlaceItem(Game1.currentLocation, placedItem, (int)cursorTile.X * 64, (int)cursorTile.Y * 64))
					{
						Game1.isCheckingNonMousePlacement = false;
						__result = true;
						return false;
					}
					if (tryToPlaceItem(Game1.currentLocation, placedItem, (int)valid_position.X, (int)valid_position.Y))
					{
						Game1.isCheckingNonMousePlacement = false;
						__result = true;
						return false;
					}
					if (!Game1.eventUp && (placedItem == null || placedItem.Stack < stack || placedItem.isPlaceable()))
					{
						isPlacingObject = true;
					}
					Game1.isCheckingNonMousePlacement = false;
				}
				if (!isPlacingObject && !was_character_at_grab_tile)
				{
					grabTile.Y += 1f;
					if (Game1.player.FacingDirection >= 0 && Game1.player.FacingDirection <= 3)
					{
						Vector2 normalized_offset = grabTile - Game1.player.Tile;
						if (normalized_offset.X > 0f || normalized_offset.Y > 0f)
						{
							normalized_offset.Normalize();
						}
						if (Vector2.Dot(Utility.DirectionsTileVectors[Game1.player.FacingDirection], normalized_offset) >= 0f && Game1.tryToCheckAt(grabTile, Game1.player))
						{
							__result = false;
							return false;
						}
					}
					if (!Game1.eventUp)
					{
						Furniture furniture = Game1.player.ActiveObject as Furniture;
						if (furniture != null)
						{
							furniture.rotate();
							Game1.playSound("dwoop", null);
							Game1.oldKBState = currentKBState;
							Game1.oldMouseState = Game1.input.GetMouseState();
							Game1.oldPadState = currentPadState;
							__result = false;
							return false;
						}
					}
					grabTile.Y -= 2f;
					if (Game1.player.FacingDirection >= 0 && Game1.player.FacingDirection <= 3 && !was_character_at_grab_tile)
					{
						Vector2 normalized_offset = grabTile - Game1.player.Tile;
						if (normalized_offset.X > 0f || normalized_offset.Y > 0f)
						{
							normalized_offset.Normalize();
						}
						if (Vector2.Dot(Utility.DirectionsTileVectors[Game1.player.FacingDirection], normalized_offset) >= 0f && Game1.tryToCheckAt(grabTile, Game1.player))
						{
							__result = false;
							return false;
						}
					}
					if (!Game1.eventUp)
					{
						Furniture furniture = Game1.player.ActiveObject as Furniture;
						if (furniture != null)
						{
							furniture.rotate();
							Game1.playSound("dwoop", null);
							Game1.oldKBState = currentKBState;
							Game1.oldMouseState = Game1.input.GetMouseState();
							Game1.oldPadState = currentPadState;
							__result = false;
							return false;
						}
					}
					grabTile = Game1.player.Tile;
					if (Game1.tryToCheckAt(grabTile, Game1.player))
					{
						__result = false;
						return false;
					}
					if (!Game1.eventUp)
					{
						Furniture furniture = Game1.player.ActiveObject as Furniture;
						if (furniture != null)
						{
							furniture.rotate();
							Game1.playSound("dwoop", null);
							Game1.oldKBState = currentKBState;
							Game1.oldMouseState = Game1.input.GetMouseState();
							Game1.oldPadState = currentPadState;
							__result = false;
							return false;
						}
					}
				}
				if (!Game1.player.isEating && Game1.player.ActiveObject != null && !Game1.dialogueUp && !Game1.eventUp && !Game1.player.canOnlyWalk && !Game1.player.FarmerSprite.PauseForSingleAnimation && !Game1.fadeToBlack && Game1.player.ActiveObject.Edibility != -300 && Game1.didPlayerJustRightClick(true))
				{
					if (Game1.player.team.SpecialOrderRuleActive("SC_NO_FOOD", null))
					{
						MineShaft mineShaft = Game1.player.currentLocation as MineShaft;
						if (mineShaft != null && mineShaft.getMineArea(-1) == 121)
						{
							Game1.addHUDMessage(new HUDMessage(Game1.content.LoadString("Strings\\StringsFromCSFiles:Object.cs.13053"), 3));
							__result = false;
							return false;
						}
					}
					if (Game1.player.hasBuff("25") && Game1.player.ActiveObject != null && !Game1.player.ActiveObject.HasContextTag("ginger_item"))
					{
						Game1.addHUDMessage(new HUDMessage(Game1.content.LoadString("Strings\\StringsFromCSFiles:Nauseous_CantEat"), 3));
						__result = false;
						return false;
					}
					Game1.player.faceDirection(2);
					Game1.player.itemToEat = Game1.player.ActiveObject;
					Game1.player.FarmerSprite.setCurrentSingleAnimation(304);
					if (Game1.objectData.TryGetValue(Game1.player.ActiveObject.ItemId, out ObjectData objectData))
					{
						Game1.currentLocation.createQuestionDialogue((objectData.IsDrink && Game1.player.ActiveObject.preserve.Value.GetValueOrDefault() != Object.PreserveType.Pickle) ? Game1.content.LoadString("Strings\\StringsFromCSFiles:Game1.cs.3159", Game1.player.ActiveObject.DisplayName) : Game1.content.LoadString("Strings\\StringsFromCSFiles:Game1.cs.3160", Game1.player.ActiveObject.DisplayName), Game1.currentLocation.createYesNoResponses(), "Eat");
					}
					Game1.oldKBState = currentKBState;
					Game1.oldMouseState = Game1.input.GetMouseState();
					Game1.oldPadState = currentPadState;
					__result = false;
					return false;
				}
			}
			if (Game1.player.CurrentTool is MeleeWeapon && Game1.player.CanMove && !Game1.player.canOnlyWalk && !Game1.eventUp && !Game1.player.onBridge.Value && Game1.didPlayerJustRightClick(true))
			{
				((MeleeWeapon)Game1.player.CurrentTool).animateSpecialMove(Game1.player);
				__result = false;
				return false;
			}
			__result = true;
			return false;
		}
	}
}