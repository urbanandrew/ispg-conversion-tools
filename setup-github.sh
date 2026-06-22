#!/bin/bash
# Quick setup script - run this after creating the GitHub repo

echo "================================================================"
echo "ISPG Conversion Tools - GitHub Setup"
echo "================================================================"
echo ""
echo "Make sure you've created the repo at:"
echo "https://github.com/new"
echo ""
echo "Repository name: ispg-conversion-tools"
echo "Visibility: Public"
echo ""
read -p "Press Enter when you've created the repo..."

echo ""
echo "Pushing code to GitHub..."
git remote add origin https://github.com/urbanandrew/ispg-conversion-tools.git
git push -u origin main

echo ""
echo "================================================================"
echo "Success!"
echo "================================================================"
echo ""
echo "Next steps:"
echo "1. Go to: https://github.com/urbanandrew/ispg-conversion-tools/actions"
echo "2. Watch the build complete (~2-3 minutes)"
echo "3. Download the installer from the Artifacts section"
echo ""
echo "Future pushes will automatically build new versions!"
echo ""
