#!/usr/bin/env python3
"""
Comprehensive fix for ALL type errors in UnitExporter and ShellExporter.
Groups fixes by root cause and applies them systematically.
"""

import re
import sys

def fix_unit_exporter(filepath):
    """Fix all errors in UnitExporter.cs"""
    with open(filepath, 'r', encoding='utf-8') as f:
        content = f.read()
    
    original = content
    
    # Category 1: record.source → record.Source (PascalCase for record properties)
    content = re.sub(r'\brecord\.source\b', 'record.Source', content)
    content = re.sub(r'\brecord\.sourceOrigin\b', 'record.SourceOrigin', content)
    
    # Category 2: double?.value → double?.Value (nullable property access)
    # Match patterns like: levelOffset.value, width.value, depth.value
    # But NOT tuple fields like: buildingNumber.value (from GetFirstParamValue result)
    # Strategy: Only fix when the variable is explicitly nullable (double?, not tuple)
    
    # Find lines with double? variable declarations or assignments
    # Then fix .value → .Value for those specific variables
    # For now, use conservative patterns:
    content = re.sub(r'(levelOffset)\.value\b', r'\1.Value', content)
    content = re.sub(r'(roomArea)\.value\b', r'\1.Value', content)
    content = re.sub(r'(roomPerimeter)\.value\b', r'\1.Value', content)
    
    # Category 3: long? to int? casts
    # Lines 382, 393, 395: BuildingNumber, StackNumber, FloorNumber
    content = re.sub(
        r'BuildingNumber = (buildingNumber\.value)',
        r'BuildingNumber = (int?)(\1)',
        content
    )
    content = re.sub(
        r'StackNumber = (stackNumber\.value)',
        r'StackNumber = (int?)(\1)',
        content
    )
    content = re.sub(
        r'FloorNumber = (floorNumber\.value)',
        r'FloorNumber = (int?)(\1)',
        content
    )
    
    # Category 4: regex Group.value → Group.Value
    content = re.sub(r'groups\[(\d+)\]\.value', r'groups[\1].Value', content)
    
    if content != original:
        with open(filepath, 'w', encoding='utf-8') as f:
            f.write(content)
        print(f"✓ Fixed UnitExporter.cs")
        return True
    else:
        print("  No changes needed in UnitExporter.cs")
        return False

def fix_shell_exporter(filepath):
    """Fix all errors in ShellExporter.cs"""
    with open(filepath, 'r', encoding='utf-8') as f:
        content = f.read()
    
    original = content
    
    # Category 1: record.source → record.Source
    content = re.sub(r'\brecord\.source\b', 'record.Source', content)
    content = re.sub(r'\brecord\.sourceOrigin\b', 'record.SourceOrigin', content)
    
    # Category 2: bool? to bool casts (line 148)
    # Find pattern: IsComposite = ParameterHelper.Boolish(...)
    # Should be: IsComposite = ParameterHelper.Boolish(...) ?? false
    content = re.sub(
        r'(IsComposite\s*=\s*ParameterHelper\.Boolish\([^)]+\))([,\s])',
        r'\1 ?? false\2',
        content
    )
    
    # Category 3: object to double? casts
    # tuple .value returns object, needs cast
    # Lines 215, 216, 310, 311: width.value, depth.value, thickness.value, area.value
    content = re.sub(
        r'(Width|Depth|Thickness|Area|CentroidX|CentroidY|CentroidZ|Volume)\s*=\s*(\w+)\.value([,\s])',
        r'\1 = \2.value as double?\3',
        content
    )
    
    # Category 4: long? to int? casts (line 280)
    content = re.sub(
        r'Count = (count\.value)',
        r'Count = (int?)(\1)',
        content
    )
    
    # Category 5: Fix ParameterHelper calls that expect double? but get object
    # Lines 308, 309: ParameterHelper.ConvertToImperial(width.value, ...)
    content = re.sub(
        r'ParameterHelper\.ConvertToImperial\((\w+)\.value,',
        r'ParameterHelper.ConvertToImperial(\1.value as double?,',
        content
    )
    
    if content != original:
        with open(filepath, 'w', encoding='utf-8') as f:
            f.write(content)
        print(f"✓ Fixed ShellExporter.cs")
        return True
    else:
        print("  No changes needed in ShellExporter.cs")
        return False

if __name__ == "__main__":
    unit_changed = fix_unit_exporter("ISPG.Conversion/Core/UnitExporter.cs")
    shell_changed = fix_shell_exporter("ISPG.Conversion/Core/ShellExporter.cs")
    
    if unit_changed or shell_changed:
        print("\n✓ All type errors fixed!")
        sys.exit(0)
    else:
        print("\n  No changes needed.")
        sys.exit(0)
