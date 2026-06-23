#!/usr/bin/env python3
"""
Comprehensive fix for tuple field access and type casts in UnitExporter and ShellExporter.
"""

import re
import sys

def fix_file(filepath):
    """Fix all tuple/type issues in one pass."""
    with open(filepath, 'r', encoding='utf-8') as f:
        content = f.read()
    
    original = content
    
    # Fix tuple field access patterns (these are TUPLE fields, not properties)
    # Tuples from GetFirstParamValue have: (value, valueString, source, paramName)
    replacements = [
        ('.Value', '.value'),        # Most common - tuple field vs property
        ('.StringValue', '.valueString'),
        ('.RawValue', '.value'),     # Legacy name
        ('.IntValue', '.value'),     # Legacy name
        ('.Source', '.source'),
        ('.ParameterName', '.paramName'),
    ]
    
    for old, new in replacements:
        # Don't replace if it's already lowercase (from a nullable double?.Value)
        # Use negative lookbehind to avoid replacing "double?.Value" or "long?.Value"
        if old == '.Value':
            # Replace .Value only when NOT preceded by ? (nullable value property)
            # Match word boundary before to avoid partial matches
            pattern = r'(?<!\?)(\.Value)\b'
            content = re.sub(pattern, '.value', content)
        else:
            content = content.replace(old, new)
    
    # Fix regex Group.value -> Group.Value (this one should be PascalCase!)
    # Match uses groups[1].value but Group is a .NET type, not our tuple
    content = re.sub(r'groups\[\d+\]\.value', lambda m: m.group(0).replace('.value', '.Value'), content)
    
    # Fix long? to int? casts
    # Pattern: SomeProperty = someTuple.value where SomeProperty expects int?
    # Common cases: BuildingNumber, StackNumber, FloorNumber
    patterns_needing_int_cast = [
        (r'(BuildingNumber\s*=\s*)(\w+\.value)\b', r'\1(\2 as long?) as int?'),
        (r'(StackNumber\s*=\s*)(\w+\.value)\b', r'\1(\2 as long?) as int?'),
        (r'(FloorNumber\s*=\s*)(\w+\.value)\b', r'\1(\2 as long?) as int?'),
    ]
    
    for pattern, replacement in patterns_needing_int_cast:
        content = re.sub(pattern, replacement, content)
    
    # Write back if changed
    if content != original:
        with open(filepath, 'w', encoding='utf-8') as f:
            f.write(content)
        print(f"✓ Fixed {filepath}")
        return True
    else:
        print(f"- No changes needed in {filepath}")
        return False

if __name__ == '__main__':
    files = [
        'ISPG.Conversion/Core/UnitExporter.cs',
        'ISPG.Conversion/Core/ShellExporter.cs',
    ]
    
    changed = []
    for f in files:
        if fix_file(f):
            changed.append(f)
    
    if changed:
        print(f"\nFixed {len(changed)} file(s): {', '.join(changed)}")
        sys.exit(0)
    else:
        print("\nNo changes needed.")
        sys.exit(1)
