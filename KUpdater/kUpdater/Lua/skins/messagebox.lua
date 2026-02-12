-- Module / Helpers
local util   = require("util.helper")

-- Konfigurationen
local background_config = {
  top_left      = "KalOnline:Frame:top_left.png",
  top_center    = "KalOnline:Frame:top_center.png",
  top_right     = "KalOnline:Frame:top_right.png",
  right_center  = "KalOnline:Frame:right_center.png",
  bottom_right  = "KalOnline:Frame:bottom_right.png",
  bottom_center = "KalOnline:Frame:bottom_center.png",
  bottom_left   = "KalOnline:Frame:bottom_left.png",
  left_center   = "KalOnline:Frame:left_center.png",
  fill_bitmap   = "KalOnline:Frame:fill_bitmap.bmp",
  fill_color    = "#101010"
}

local layout_config = {
  top_width_offset    = 7,
  bottom_width_offset = 15,
  left_height_offset  = 5,
  right_height_offset = 5,
  fill_pos_offset     = 5,
  fill_width_offset   = 12,
  fill_height_offset  = 9
}


-- Rückgabe der Fensterdefinition
return {
  background = background_config,
  layout     = layout_config,

  init = function()


--print("button count:", msg_buttons and #msg_buttons or 0)
--for i,v in ipairs(msg_buttons or {}) do print(i, v) end
--print("msg_default:", msg_default)


    local titleLabel = Label(
      "lb_title",
      util.make_bounds(35, 0, 200, 40),
      msg_title,
      Font("Chiller", 40, "Italic"),
      Color.Orange
    )
    Controls.Add(titleLabel)

    local messageTextBox = TextBox(
      "tb_changelog",
      util.full_anchor(36, 55, 36, 70),
      msg_text,
      Font("Segoe UI", 10, "Regular"),
      Color.White,
      MakeColor.FromHex("#101010"),
      true, true, MakeColor.FromHex("#7C6E4B")
    )
    messageTextBox.BorderColor     = MakeColor.FromHex("#7C6E4B")
    messageTextBox.BorderThickness = 3
    messageTextBox.GlowEnabled     = true
    messageTextBox.GlowColor       = Color.White
    messageTextBox.GlowRadius      = 6
    Controls.Add(messageTextBox)



    -- dynamische, rechtsbündige Buttons (erste an -140,-55, weitere links davon)
    local buttons = msg_buttons or {"OK"}
    local default = (msg_default and type(msg_default) == "string" and msg_default) or buttons[1]

    local btnWidth, btnHeight, spacing = 97, 22, 8
    local firstX, firstY = -140, -55

    local function add_button_at(id, x, y, w, h, text, onClick)
      Controls.Add(Button(
        id,
        util.make_bounds(x, y, w, h),
        text,
        Font("Segoe UI", 11, "Regular"),
        Color.Orange,
        "KalOnline:Buttons",
        onClick
      ))
    end

    if #buttons == 1 then
    -- genau ein Button: an der alten Position platzieren
    local name = buttons[1]
    add_button_at("btn_default", firstX, firstY, btnWidth, btnHeight, name,
      function() close_with_result(name) end)
    else
      -- mehrere Buttons: erste an firstX, weitere links davon
      for i, name in ipairs(buttons) do
        local x = firstX - (i - 1) * (btnWidth + spacing)
        add_button_at("btn_default", x, firstY, btnWidth, btnHeight, name,
          function() close_with_result(name) end)
      end
    end

  end
}
