#!/bin/bash
BUILD_DIR="Builds/WebGL"

if [ ! -f "$BUILD_DIR/index.html" ]; then
    echo "Error: WebGL build not found in $BUILD_DIR"
    echo "Please build the project first using Unity Menu: Build > Build WebGL"
    exit 1
fi

echo "Starting Web Server at http://localhost:8000"
echo "Press Ctrl+C to stop."
# Try to open the browser (macOS 'open', Linux 'xdg-open', Windows 'start')
if [[ "$OSTYPE" == "darwin"* ]]; then
    open "http://localhost:8000"
elif [[ "$OSTYPE" == "linux-gnu"* ]]; then
    xdg-open "http://localhost:8000"
fi

cd "$BUILD_DIR" && python3 -m http.server 8000
