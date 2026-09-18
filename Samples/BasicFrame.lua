local frame = CreateFrame("Frame", "BasicFrame", UIParent)
frame:SetSize(500, 300)
frame:SetPoint("CENTER", UIParent, "CENTER", 0, 0)
frame:SetBackdrop({ bgFile = "Interface\\DialogFrame\\UI-DialogBox-Background", edgeFile = "Interface\\Tooltips\\UI-Tooltip-Border" })

local title = frame:CreateFontString(nil, "OVERLAY")
title:SetPoint("TOP", frame, "TOP", 0, -24)
title:SetText("Basic TBC Frame")
