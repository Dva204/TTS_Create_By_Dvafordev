import re

with open("Form1.cs", "r", encoding="utf-8") as f:
    code = f.read()

# We need to extract all methods, group them, and then reassemble.
# A C# method in this file starts with "    private" or "    public" and ends with "    }" where the next line is blank or another method.
# Since regex for balanced brackets is hard, let's use a simpler approach.

import ast

def extract_methods(code):
    methods = []
    lines = code.split('\n')
    current_method = []
    in_method = False
    brace_level = 0
    method_name = ""
    
    header = []
    footer = []
    
    i = 0
    while i < len(lines):
        line = lines[i]
        if not in_method:
            # Check if this line starts a method
            match = re.match(r'^\s*(?:private|public|protected) (?:async )?(?:void|Task|string|int|bool) ([A-Za-z0-9_]+)\(', line)
            if match and "Form1()" not in line: # Exclude constructor from being moved
                in_method = True
                brace_level = 0
                method_name = match.group(1)
                current_method.append(line)
            else:
                header.append(line)
        else:
            current_method.append(line)
            
        if in_method:
            brace_level += line.count('{')
            brace_level -= line.count('}')
            if brace_level == 0 and '{' in '\n'.join(current_method):
                methods.append({'name': method_name, 'lines': current_method})
                in_method = False
                current_method = []
                
        i += 1
                
    # footer is whatever is left after the last method in header, actually header contains everything if we just pop the methods out.
    # Wait, the last line is `}` which closes the class. It will be in `header`.
    return header, methods

header, methods = extract_methods(code)

groups = {
    "Form Lifecycle & Common": ["Form1_Load", "Form1_FormClosing", "SetStatus", "RefreshStatus", "RefreshVoices", "Monitor", "WriteLog", "SetBusy", "ShowError", "CleanupAudio"],
    "TAB: TTS / Batch": ["AddVoiceButton_Click", "GenerateButton_Click", "StopButton_Click", "PlayButton_Click", "SaveWavButton_Click", "BtnRunBatch_Click", "BtnJoinMp3_Click", "BtnBrowseOutputFolder_Click", "BtnImportFile_Click"],
    "TAB: Video Editor": ["BtnBrowseVideoAudio_Click", "BtnBrowseVideoSrt_Click", "BtnBrowseImageFolder_Click", "BtnAnalyzeTimeline_Click", "BtnRenderVideo_Click"],
    "TAB: Voice Library": ["BtnGeneratePreview_Click", "BtnUseSelectedVoice_Click", "BtnCopyVoiceId_Click", "BtnReplaceReference_Click", "BtnDeleteVoice_Click", "DgvVoiceLibrary_SelectionChanged", "BtnPlayReference_Click", "BtnLibrarySearch_Click", "BtnLibraryRefresh_Click"]
}

# Group them
grouped_methods = {k: [] for k in groups.keys()}
ungrouped = []

for m in methods:
    found = False
    for g_name, g_list in groups.items():
        if m['name'] in g_list:
            grouped_methods[g_name].append(m)
            found = True
            break
    if not found:
        ungrouped.append(m)

# Build new code
new_code_lines = []
# The header contains the class start, fields, and constructor, AND the closing brace.
# We should insert the methods right before the last closing brace.
# Find the last closing brace in header
last_brace_idx = -1
for i in range(len(header)-1, -1, -1):
    if header[i].strip() == '}':
        last_brace_idx = i
        break

if last_brace_idx != -1:
    new_code_lines.extend(header[:last_brace_idx])
else:
    new_code_lines.extend(header)

for g_name, m_list in grouped_methods.items():
    if not m_list: continue
    new_code_lines.append("")
    new_code_lines.append(f"    // ==========================================")
    new_code_lines.append(f"    // {g_name}")
    new_code_lines.append(f"    // ==========================================")
    new_code_lines.append(f"    #region {g_name}")
    for m in m_list:
        new_code_lines.append("")
        new_code_lines.extend(m['lines'])
    new_code_lines.append(f"    #endregion")
    new_code_lines.append("")

if ungrouped:
    new_code_lines.append(f"    // ==========================================")
    new_code_lines.append(f"    // Other")
    new_code_lines.append(f"    // ==========================================")
    new_code_lines.append(f"    #region Other")
    for m in ungrouped:
        new_code_lines.append("")
        new_code_lines.extend(m['lines'])
    new_code_lines.append(f"    #endregion")
    new_code_lines.append("")

if last_brace_idx != -1:
    new_code_lines.extend(header[last_brace_idx:])

with open("Form1.cs", "w", encoding="utf-8") as f:
    f.write('\n'.join(new_code_lines))

