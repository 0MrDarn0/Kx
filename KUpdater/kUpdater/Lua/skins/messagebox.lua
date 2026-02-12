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
  fill_width_offset   = 12, --12,
  fill_height_offset  = 9
}


-- Rückgabe der Fensterdefinition
return {
  background = background_config,
  layout     = layout_config,

  init = function()

-- Title
    local titleLabel = Label(
      "lb_title",
      util.make_bounds(35, 0, 200, 40),
      msg_title,
      Font("Chiller", 40, "Italic"),
      Color.Orange
    )
    Controls.Add(titleLabel)

    -- Changelog TextBox
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

    -- Start Button
    local okBtn = Button(
      "btn_default",
      util.make_bounds(-140, -55, 97, 22),
      "OK",
      Font("Segoe UI", 11, "Regular"),
      Color.Orange,
      "KalOnline:Buttons",
      function()  end
    )
    Controls.Add(okBtn)

        -- Exit Button
    local exitBtn = Button(
      "btn_exit",
      util.make_bounds(-35, 16, 18, 18),
      T("button.exit"),
      Font("Segoe UI", 10, "Regular"),
      Color.Orange,
      "KalOnline:Buttons",
      function() close_window() end
    )
    Controls.Add(exitBtn)

  end
}
