#!/bin/bash

# Linux script to build the mod and copy it to our mod pack
# and then start the game for quick testing

# requires dotnet, r2modman, xdotool, and X11 display server


killall -9 r2modmanPlus

profile="Weeee"

rm /home/$USER/.config/r2modmanPlus-local/RiskOfRain2/profiles/$profile/BepInEx/plugins/FuzzyExpress-CursorHP/CursorHP.dll

cd /home/$USER/Documents/RiskOfRain2/CursorHP && dotnet build

if [ $? -ne 0 ]; then
    echo "Build failed, exiting..."
    exit 1
fi

# Copy the mod to the mod pack
cp /home/$USER/Documents/RiskOfRain2/CursorHP/CursorHP/bin/Debug/netstandard2.1/CursorHP.dll /home/$USER/.config/r2modmanPlus-local/RiskOfRain2/profiles/$profile/BepInEx/plugins/FuzzyExpress-CursorHP/
cp /home/$USER/Documents/RiskOfRain2/CursorHP/CursorHP/manifest.json /home/$USER/.config/r2modmanPlus-local/RiskOfRain2/profiles/$profile/BepInEx/plugins/FuzzyExpress-CursorHP/


# Function to parse window position using regex
get_window_position() {
    local window_id=$1
    local window_geometry

    # Get window geometry
    window_geometry=$(xdotool getwindowgeometry "$window_id" 2>/dev/null)
    
    # Extract position line
    local position_line
    position_line=$(echo "$window_geometry" | grep "Position:" 2>/dev/null)
    
    # Extract coordinates with regex
    if [[ -n "$position_line" ]]; then
        local position_part
        position_part=$(echo "$position_line" | grep -o '[0-9]\+,[0-9]\+' 2>/dev/null)
        
        if [[ -n "$position_part" ]]; then
            WINDOW_X=$(echo "$position_part" | cut -d',' -f1)
            WINDOW_Y=$(echo "$position_part" | cut -d',' -f2)
            
            # Validate coordinates (check if they seem reasonable)
            if [[ "$WINDOW_X" == "10" && "$WINDOW_Y" == "10" ]]; then
                echo "Warning: Suspicious coordinates detected (10,10), using manual coordinates"
                WINDOW_X=$MANUAL_WINDOW_X
                WINDOW_Y=$MANUAL_WINDOW_Y
                return 1
            fi
            
            echo "Window position: $WINDOW_X,$WINDOW_Y"
            return 0
        fi
    fi
    
    echo "Failed to get window position, using manual coordinates"
    WINDOW_X=$MANUAL_WINDOW_X
    WINDOW_Y=$MANUAL_WINDOW_Y
    return 1
}


# Function to click at a position relative to window
click_relative() {
    local window_id=$1
    local x_offset=$2
    local y_offset=$3
    local use_manual=${4:-0}
    
    # Get/use window position
    if [[ $use_manual -eq 1 ]]; then
        WINDOW_X=$MANUAL_WINDOW_X
        WINDOW_Y=$MANUAL_WINDOW_Y
    else
        get_window_position "$window_id"
    fi
    
    # Calculate absolute click position
    local click_x=$((WINDOW_X + x_offset))
    local click_y=$((WINDOW_Y + y_offset))
    
    echo "Clicking at: $click_x,$click_y (offset $x_offset,$y_offset from window at $WINDOW_X,$WINDOW_Y)"
    
    # Move mouse and click
    xdotool mousemove $click_x $click_y
    sleep 0.5
    xdotool click 1
    sleep 0.5
}

# Launch r2modman
echo "Launching r2modman..."
r2modman &




# Wait for r2modman to open
sleep 3

# Get window ID
echo "Finding r2modman window..."
WINDOW_ID=$(xdotool search --name "r2modman (3*)" | head -n 1)

if [[ -z "$WINDOW_ID" ]]; then
    echo "Could not find r2modman window. Make sure it's open."
    exit 1
fi



echo "Found window ID: $WINDOW_ID"

xdotool windowraise $WINDOW_ID
xdotool windowactivate $WINDOW_ID

# Click on Play button
echo "Clicking Play button..."
click_relative "$WINDOW_ID" 420 380

# Wait for play dialog to appear
sleep .3

# Click on Default button
echo "Clicking Default button..."
click_relative "$WINDOW_ID" 100 64

echo "Done! Game should be launching."