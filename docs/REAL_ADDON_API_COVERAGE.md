# Real Addon API Coverage

Generated from the workspace corpus; counts below are scanner observations, not estimates.

- Source folders: 38
- Lua files: 940
- XML files: 371
- TOC files: 57
- Unique widget methods: 1513
- Unique global calls: 11581
- AIO handlers: 54
- Compatibility definitions: 95

## Classifications

- FrameXmlLua: 335
- FrameXmlXml: 291
- UnknownLua: 285
- NormalAddon: 238
- AioServer: 58
- AioClient: 58
- Toc: 57
- GlueXmlXml: 23
- GlueXmlLua: 19
- CompatibilityLayer: 4

## Widget methods

| Name | Calls | Files | Sources |
|---|---:|---:|---:|
| `SetPoint` | 6920 | 358 | 38 |
| `Hide` | 4476 | 444 | 38 |
| `SetText` | 5301 | 381 | 37 |
| `Show` | 4005 | 431 | 38 |
| `SetWidth` | 1627 | 273 | 36 |
| `SetHeight` | 1367 | 268 | 35 |
| `SetScript` | 2562 | 258 | 37 |
| `SetTexture` | 1952 | 262 | 36 |
| `CreateFontString` | 751 | 137 | 35 |
| `CreateTexture` | 552 | 149 | 35 |
| `IsShown` | 1526 | 247 | 20 |
| `GetName` | 1454 | 193 | 25 |
| `RegisterEvent` | 1539 | 231 | 15 |
| `GetText` | 785 | 176 | 31 |
| `ClearAllPoints` | 809 | 191 | 24 |
| `SetSize` | 754 | 79 | 20 |
| `Disable` | 735 | 183 | 26 |
| `Enable` | 661 | 173 | 25 |
| `SetTextColor` | 666 | 140 | 29 |
| `SetVertexColor` | 728 | 161 | 22 |
| `sub` | 857 | 82 | 32 |
| `SetJustifyH` | 427 | 124 | 33 |
| `SetBackdrop` | 171 | 99 | 34 |
| `SetOwner` | 279 | 143 | 33 |
| `AddLine` | 598 | 90 | 23 |
| `SetValue` | 408 | 142 | 20 |
| `EnableMouse` | 266 | 109 | 36 |
| `SetAllPoints` | 284 | 118 | 30 |
| `GetWidth` | 450 | 148 | 13 |
| `GetParent` | 679 | 113 | 11 |
| `GetID` | 703 | 132 | 9 |
| `SetAlpha` | 472 | 113 | 14 |
| `GetHeight` | 436 | 123 | 13 |
| `gsub` | 248 | 90 | 31 |
| `SetTexCoord` | 460 | 122 | 12 |
| `SetBackdropColor` | 211 | 90 | 33 |
| `match` | 200 | 85 | 32 |
| `SetParent` | 155 | 75 | 15 |
| `GetUInt32` | 948 | 33 | 15 |
| `find` | 385 | 66 | 17 |
| `ClearFocus` | 204 | 90 | 23 |
| `AddMessage` | 255 | 77 | 20 |
| `SetBackdropBorderColor` | 161 | 80 | 30 |
| `SetChecked` | 335 | 62 | 17 |
| `SetMinMaxValues` | 173 | 96 | 20 |
| `SetFrameStrata` | 180 | 85 | 20 |
| `SendBroadcastMessage` | 458 | 46 | 14 |
| `RegisterForDrag` | 95 | 80 | 35 |
| `GetGUIDLow` | 383 | 41 | 16 |
| `SetFontObject` | 150 | 72 | 23 |

## Global calls

| Name | Calls | Files | Sources |
|---|---:|---:|---:|
| `CreateFrame` | 1516 | 283 | 38 |
| `getglobal` | 3645 | 179 | 6 |
| `print` | 918 | 123 | 24 |
| `format` | 797 | 91 | 8 |
| `tinsert` | 744 | 92 | 8 |
| `PlaySound` | 429 | 158 | 7 |
| `GetSpellInfo` | 438 | 59 | 12 |
| `UIDropDownMenu_AddButton` | 510 | 79 | 7 |
| `floor` | 303 | 87 | 9 |
| `GetTime` | 238 | 59 | 16 |
| `GetItemInfo` | 224 | 51 | 18 |
| `jsonSkip` | 250 | 31 | 25 |
| `VALUES` | 344 | 33 | 17 |
| `gsub` | 991 | 26 | 6 |
| `LibStub` | 292 | 166 | 3 |
| `UIDropDownMenu_Initialize` | 200 | 86 | 8 |
| `s` | 256 | 34 | 15 |
| `normalizeIncoming` | 156 | 29 | 23 |
| `GetLocale` | 177 | 88 | 6 |
| `jsonParseValue` | 115 | 30 | 24 |
| `ShowUIPanel` | 116 | 84 | 8 |
| `StaticPopup_Show` | 198 | 49 | 8 |
| `UIDropDownMenu_CreateInfo` | 250 | 76 | 4 |
| `UnitName` | 170 | 68 | 6 |
| `jsonParseString` | 90 | 30 | 24 |
| `min` | 145 | 55 | 8 |
| `createButton` | 330 | 16 | 12 |
| `GetCVar` | 255 | 59 | 4 |
| `max` | 159 | 49 | 7 |
| `extractPayloadFromArgs` | 109 | 25 | 20 |
| `HideUIPanel` | 144 | 75 | 5 |
| `setStatus` | 213 | 18 | 13 |
| `strsub` | 153 | 42 | 7 |
| `GetScreenWidth` | 104 | 47 | 9 |
| `jsonParseNumber` | 60 | 30 | 24 |
| `num` | 519 | 9 | 8 |
| `createLabel` | 551 | 9 | 7 |
| `IsPlayer` | 149 | 20 | 11 |
| `GetScreenHeight` | 87 | 41 | 9 |
| `GtTraslater` | 1716 | 18 | 1 |
| `decodeMaybeJson` | 54 | 27 | 21 |
| `IsShiftKeyDown` | 58 | 35 | 14 |
| `ceil` | 113 | 46 | 5 |
| `RegisterPlayerEvent` | 63 | 34 | 12 |
| `UnitLevel` | 108 | 47 | 5 |
| `asArray` | 195 | 12 | 10 |
| `asNumber` | 562 | 8 | 5 |
| `strmatch` | 103 | 34 | 6 |
| `GetItemIcon` | 45 | 29 | 16 |
| `text` | 285 | 9 | 8 |

## Frame types

| Name | Calls | Files | Sources |
|---|---:|---:|---:|
| `Frame` | 3367 | 424 | 38 |
| `Button` | 4034 | 376 | 35 |
| `Texture` | 7228 | 243 | 6 |
| `FontString` | 2590 | 239 | 6 |
| `CheckButton` | 1275 | 117 | 20 |
| `EditBox` | 183 | 121 | 28 |
| `ScrollFrame` | 200 | 114 | 24 |
| `StatusBar` | 190 | 79 | 15 |
| `Slider` | 171 | 66 | 8 |
| `FRAME` | 80 | 44 | 3 |
| `BUTTON` | 63 | 30 | 4 |
| `frame` | 27 | 18 | 1 |
| `GameTooltip` | 9 | 7 | 5 |
| `Editbox` | 5 | 5 | 5 |
| `PlayerModel` | 6 | 6 | 3 |
| `DressUpModel` | 3 | 3 | 3 |
| `Fontstring` | 4 | 4 | 1 |
| `EDITBOX` | 3 | 3 | 1 |
| `editbox` | 3 | 3 | 1 |
| `STATUSBAR` | 1 | 1 | 1 |
| `Colorselect` | 1 | 1 | 1 |
| `COOLDOWN` | 1 | 1 | 1 |

## Templates

| Name | Calls | Files | Sources |
|---|---:|---:|---:|
| `UIPanelButtonTemplate` | 394 | 132 | 32 |
| `UIPanelCloseButton` | 149 | 115 | 25 |
| `GameFontNormalSmall` | 454 | 132 | 5 |
| `GameFontNormal` | 468 | 128 | 5 |
| `GameFontHighlight` | 402 | 122 | 4 |
| `GameFontHighlightSmall` | 429 | 108 | 4 |
| `UIDropDownMenuTemplate` | 231 | 80 | 7 |
| `UIPanelScrollFrameTemplate` | 65 | 50 | 19 |
| `InputBoxTemplate` | 57 | 40 | 17 |
| `UICheckButtonTemplate` | 36 | 24 | 16 |
| `GameFontNormalLarge` | 88 | 21 | 3 |
| `CooldownFrameTemplate` | 44 | 26 | 4 |
| `FauxScrollFrameTemplate` | 35 | 25 | 5 |
| `GameMenuButtonTemplate` | 65 | 21 | 3 |
| `GameFontDisableSmall` | 71 | 19 | 3 |
| `SecureFrameTemplate` | 49 | 27 | 3 |
| `NumberFontNormal` | 39 | 29 | 3 |
| `SmallMoneyFrameTemplate` | 46 | 24 | 3 |
| `OptionsCheckButtonTemplate` | 72 | 15 | 3 |
| `TextStatusBarText` | 42 | 24 | 3 |
| `StatFrameTemplate` | 98 | 10 | 3 |
| `TextStatusBar` | 44 | 22 | 3 |
| `InterfaceOptionsCheckButtonTemplate` | 176 | 5 | 3 |
| `ChatFontNormal` | 37 | 23 | 3 |
| `UIPanelScrollBarTemplate` | 26 | 19 | 5 |
| `ItemButtonTemplate` | 36 | 22 | 3 |
| `QuestFont` | 78 | 14 | 2 |
| `GameFontBlack` | 43 | 13 | 3 |
| `OptionsButtonTemplate` | 42 | 12 | 3 |
| `MagicResistanceFrameTemplate` | 50 | 10 | 3 |
| `UIPanelButtonTemplate2` | 28 | 10 | 5 |
| `SearchBoxMineTemplate` | 42 | 32 | 1 |
| `GameTooltipTemplate` | 14 | 13 | 7 |
| `UIMenuButtonStretchTemplateB` | 45 | 27 | 1 |
| `PartyBuffButtonTemplate` | 68 | 8 | 2 |
| `collections-newglow` | 61 | 17 | 1 |
| `QuestTitleButtonTemplate` | 82 | 6 | 2 |
| `UIPanelButtonHighlightTexture` | 36 | 9 | 3 |
| `WardrobeItemsModelTemplate` | 190 | 5 | 1 |
| `CharacterFrameTabButtonTemplate` | 28 | 11 | 3 |
| `GameFontNormalMed3` | 129 | 7 | 1 |
| `transmog-wardrobe-border-selected-wisp` | 180 | 5 | 1 |
| `GameFontDisable` | 22 | 13 | 3 |
| `GameFontWhite` | 22 | 12 | 3 |
| `OptionsSliderTemplate` | 29 | 9 | 3 |
| `TabButtonTemplate` | 31 | 8 | 3 |
| `QuestTitleFont` | 36 | 10 | 2 |
| `InsetFrameTemplate` | 38 | 17 | 1 |
| `ToySpellButtonTemplate` | 75 | 7 | 1 |
| `QuestItemTemplate` | 37 | 7 | 2 |

## Events

| Name | Calls | Files | Sources |
|---|---:|---:|---:|
| `PLAYER_ENTERING_WORLD` | 93 | 77 | 10 |
| `PLAYER_LOGIN` | 42 | 19 | 8 |
| `VARIABLES_LOADED` | 31 | 22 | 5 |
| `PARTY_MEMBERS_CHANGED` | 30 | 26 | 4 |
| `DISPLAY_SIZE_CHANGED` | 25 | 21 | 3 |
| `PLAYER_REGEN_ENABLED` | 17 | 15 | 6 |
| `UNIT_PET` | 20 | 17 | 4 |
| `ADDON_LOADED` | 18 | 16 | 4 |
| `PLAYER_REGEN_DISABLED` | 14 | 13 | 6 |
| `UNIT_AURA` | 15 | 12 | 6 |
| `BAG_UPDATE` | 24 | 15 | 3 |
| `RAID_ROSTER_UPDATE` | 15 | 15 | 4 |
| `CVAR_UPDATE` | 17 | 16 | 3 |
| `PARTY_LEADER_CHANGED` | 15 | 15 | 3 |
| `UNIT_NAME_UPDATE` | 14 | 12 | 4 |
| `CHARACTER_POINTS_CHANGED` | 10 | 10 | 5 |
| `PLAYER_LEVEL_UP` | 11 | 9 | 5 |
| `MODIFIER_STATE_CHANGED` | 9 | 9 | 6 |
| `PLAYER_TARGET_CHANGED` | 14 | 11 | 3 |
| `UNIT_LEVEL` | 12 | 12 | 3 |
| `SPELL_UPDATE_COOLDOWN` | 10 | 10 | 4 |
| `SPELLS_CHANGED` | 11 | 9 | 4 |
| `PLAYER_FLAGS_CHANGED` | 11 | 8 | 4 |
| `UNIT_MODEL_CHANGED` | 9 | 9 | 4 |
| `UNIT_FACTION` | 9 | 9 | 4 |
| `UPDATE_BINDINGS` | 11 | 9 | 3 |
| `READY_CHECK` | 9 | 9 | 3 |
| `MEETINGSTONE_CHANGED` | 10 | 6 | 4 |
| `MERCHANT_UPDATE` | 9 | 8 | 3 |
| `UNIT_ATTACK_SPEED` | 7 | 7 | 4 |
| `UNIT_PORTRAIT_UPDATE` | 8 | 8 | 3 |
| `UNIT_INVENTORY_CHANGED` | 8 | 6 | 4 |
| `ACTIONBAR_PAGE_CHANGED` | 9 | 7 | 3 |
| `UNIT_STATS` | 8 | 7 | 3 |
| `PLAYER_FOCUS_CHANGED` | 8 | 7 | 3 |
| `READY_CHECK_CONFIRM` | 7 | 7 | 3 |
| `UPDATE_FACTION` | 7 | 7 | 3 |
| `READY_CHECK_FINISHED` | 7 | 7 | 3 |
| `ZONE_CHANGED` | 7 | 7 | 3 |
| `PET_BAR_UPDATE` | 7 | 7 | 3 |
| `UPDATE_INVENTORY_ALERTS` | 7 | 7 | 3 |
| `PLAYER_PVP_RANK_CHANGED` | 7 | 7 | 3 |
| `ITEM_LOCK_CHANGED` | 7 | 7 | 3 |
| `WORLD_MAP_UPDATE` | 6 | 6 | 4 |
| `ZONE_CHANGED_NEW_AREA` | 6 | 6 | 4 |
| `PLAYER_ENTER_COMBAT` | 6 | 6 | 4 |
| `BAG_UPDATE_COOLDOWN` | 6 | 6 | 4 |
| `UNIT_ATTACK` | 6 | 6 | 3 |
| `UNIT_ATTACK_POWER` | 6 | 6 | 3 |
| `UNIT_RANGEDDAMAGE` | 6 | 6 | 3 |

## Compatibility APIs

- `Clamp` — 3 evidence record(s)
- `CopyTable` — 2 evidence record(s)
- `CreateColor` — 2 evidence record(s)
- `DeltaLerp` — 1 evidence record(s)
- `FrameDeltaLerp` — 1 evidence record(s)
- `GetButtonState` — 1 evidence record(s)
- `GetCursorPosition` — 1 evidence record(s)
- `GetEffectiveScale` — 1 evidence record(s)
- `GetHorizontalScroll` — 1 evidence record(s)
- `GetMinMaxValues` — 1 evidence record(s)
- `GetNumGroupMembers` — 1 evidence record(s)
- `GetNumSubgroupMembers` — 1 evidence record(s)
- `getprinthandler` — 1 evidence record(s)
- `GetRect` — 1 evidence record(s)
- `GetScrollChild` — 1 evidence record(s)
- `GetSize` — 1 evidence record(s)
- `GetSpellBookID` — 1 evidence record(s)
- `GetSpellBookItemInfo` — 1 evidence record(s)
- `GetSpellCooldown` — 1 evidence record(s)
- `GetStatusBarTexture` — 1 evidence record(s)
- `GetStringHeight` — 1 evidence record(s)
- `GetStringWidth` — 1 evidence record(s)
- `GetValue` — 1 evidence record(s)
- `GetValueStep` — 1 evidence record(s)
- `GetVerticalScroll` — 1 evidence record(s)
- `HasScript` — 1 evidence record(s)
- `HighlightText` — 1 evidence record(s)
- `ipairs_reverse` — 1 evidence record(s)
- `IsInGroup` — 1 evidence record(s)
- `IsInRaid` — 1 evidence record(s)
- `IsSpellInRange` — 1 evidence record(s)
- `IsSpellKnown` — 3 evidence record(s)
- `IsUsableSpell` — 1 evidence record(s)
- `Lerp` — 3 evidence record(s)
- `noop` — 1 evidence record(s)
- `print` — 3 evidence record(s)
- `PrintTab` — 1 evidence record(s)
- `Round` — 1 evidence record(s)
- `Saturate` — 1 evidence record(s)
- `SetAlpha` — 1 evidence record(s)
- `SetAutoFocus` — 1 evidence record(s)
- `SetBlendMode` — 1 evidence record(s)
- `SetButtonState` — 1 evidence record(s)
- `SetClampedToScreen` — 1 evidence record(s)
- `SetCursorPosition` — 1 evidence record(s)
- `SetDisabledTexture` — 1 evidence record(s)
- `SetFontObject` — 1 evidence record(s)
- `SetGradient` — 1 evidence record(s)
- `SetHighlightTexture` — 1 evidence record(s)
- `SetHorizontalScroll` — 1 evidence record(s)
- `SetHorizTile` — 1 evidence record(s)
- `SetCheckedTexture` — 1 evidence record(s)
- `SetJustifyH` — 1 evidence record(s)
- `SetJustifyV` — 1 evidence record(s)
- `SetMaxLetters` — 1 evidence record(s)
- `SetMinMaxValues` — 1 evidence record(s)
- `SetMovable` — 1 evidence record(s)
- `SetNormalTexture` — 1 evidence record(s)
- `SetNumeric` — 1 evidence record(s)
- `SetObeyStepOnDrag` — 1 evidence record(s)
- `SetOrientation` — 1 evidence record(s)
- `setprinthandler` — 1 evidence record(s)
- `SetPushedTexture` — 1 evidence record(s)
- `SetResizable` — 1 evidence record(s)
- `SetScrollChild` — 1 evidence record(s)
- `SetShadowColor` — 1 evidence record(s)
- `SetShadowOffset` — 1 evidence record(s)
- `SetSize` — 1 evidence record(s)
- `SetSpacing` — 1 evidence record(s)
- `SetStatusBarColor` — 1 evidence record(s)
- `SetStatusBarTexture` — 1 evidence record(s)
- `SetTexCoord` — 1 evidence record(s)
- `SetTextInsets` — 1 evidence record(s)
- `SetThumbTexture` — 1 evidence record(s)
- `SetUserPlaced` — 1 evidence record(s)
- `SetValue` — 1 evidence record(s)
- `SetValueStep` — 1 evidence record(s)
- `SetVertexColor` — 1 evidence record(s)
- `SetVerticalScroll` — 1 evidence record(s)
- `SetVertTile` — 1 evidence record(s)
- `SetWordWrap` — 1 evidence record(s)
- `strjoin` — 2 evidence record(s)
- `strsplit` — 2 evidence record(s)
- `strtrim` — 2 evidence record(s)
- `tContains` — 2 evidence record(s)
- `tIndexOf` — 1 evidence record(s)
- `tInvert` — 3 evidence record(s)
- `tostringall` — 1 evidence record(s)
- `UIDropDownMenu_SetWidth` — 1 evidence record(s)
- `UnitAura` — 1 evidence record(s)
- `UnitInVehicle` — 1 evidence record(s)
- `UnitPower` — 1 evidence record(s)
- `UnitPowerMax` — 1 evidence record(s)
- `UpdateScrollChildRect` — 1 evidence record(s)
- `wipe` — 3 evidence record(s)
