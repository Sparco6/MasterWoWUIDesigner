local panel = CreateFrame("Frame", "DynamicPanel", UIParent)
panel:SetSize(360, 320)
panel:SetPoint("CENTER", UIParent, "CENTER", 0, 0)

for i = 1, 8 do
    local button = CreateFrame("Button", "DynamicButton" .. i, panel)
    button:SetSize(240, 28)
    button:SetPoint("TOP", panel, "TOP", 0, -(20 + i * 32))
    button:SetText("Dynamic row " .. i)
end
