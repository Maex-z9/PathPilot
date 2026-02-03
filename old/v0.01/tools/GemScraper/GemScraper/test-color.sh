#!/bin/bash
curl -s "https://www.poewiki.net/wiki/Fireball" > fireball.html
echo "=== Searching for 'Intelligence' ==="
grep -i "intelligence" fireball.html | head -5
echo ""
echo "=== Searching for 'Primary attribute' ==="
grep -i "primary" fireball.html | head -5
echo ""
echo "=== Searching for gem color classes ==="
grep -i "tc -" fireball.html | head -10
echo ""
echo "=== Searching in infobox ==="
grep -A 50 'class="infobox' fireball.html | grep -i -E "strength|dexterity|intelligence" | head -5
