import re
import sys

BASE = "/home/runner/work/0install-dotnet/0install-dotnet/"

def read(path):
    with open(BASE + path, 'r') as f:
        return f.read()

def write(path, content):
    with open(BASE + path, 'w') as f:
        f.write(content)

def add_attribute_before_class(content, class_name, attribute="[FastClonerClonable]"):
    """Add [FastClonerClonable] on the line before the class declaration."""
    # Match line with class declaration (may have partial, sealed, abstract, etc.)
    pattern = r'([ \t]*(?:public\s+(?:sealed\s+|abstract\s+)?(?:partial\s+)?class\s+' + re.escape(class_name) + r'\b))'
    def replacer(m):
        line = m.group(1)
        indent = re.match(r'^([ \t]*)', line).group(1)
        return indent + attribute + '\n' + line
    new_content, count = re.subn(pattern, replacer, content, count=1)
    if count == 0:
        print(f"WARNING: Could not find class {class_name}", file=sys.stderr)
    return new_content

def replace_simple_clone(content, old_pattern, new_body="this.FastDeepClone()"):
    """Replace a Clone method body."""
    new_content = content.replace(old_pattern, new_body, 1)
    if new_content == content:
        print(f"WARNING: Could not find pattern: {old_pattern[:60]}", file=sys.stderr)
    return new_content

def replace_multiline_clone(content, start_pattern, new_line):
    """Replace a multi-line Clone method (from start_pattern to matching ';')."""
    idx = content.find(start_pattern)
    if idx == -1:
        print(f"WARNING: Could not find: {start_pattern[:60]}", file=sys.stderr)
        return content
    # Find the end of the method (the ';' after the closing brace)
    end_idx = idx
    brace_count = 0
    in_method = False
    i = idx + len(start_pattern)
    # Look for ';' or '{...}' ending
    while i < len(content):
        c = content[i]
        if c == '{':
            brace_count += 1
            in_method = True
        elif c == '}':
            brace_count -= 1
            if in_method and brace_count == 0:
                # Find the ';' after this
                j = i + 1
                while j < len(content) and content[j] in ' \t\r\n':
                    j += 1
                if j < len(content) and content[j] == ';':
                    end_idx = j + 1
                else:
                    end_idx = i + 1
                break
        elif c == ';' and not in_method:
            end_idx = i + 1
            break
        i += 1
    
    return content[:idx] + new_line + content[end_idx:]

def remove_region_block(content, region_name):
    """Remove an entire #region ... #endregion block."""
    pattern = r'[ \t]*#region ' + re.escape(region_name) + r'.*?#endregion\s*\n?'
    new_content, count = re.subn(pattern, '', content, flags=re.DOTALL)
    if count == 0:
        print(f"WARNING: Could not remove region: {region_name}", file=sys.stderr)
    return new_content

def remove_explicit_interface_method(content, method_start):
    """Remove an explicit interface method from its start to its ending ;"""
    idx = content.find(method_start)
    if idx == -1:
        print(f"WARNING: Could not find method: {method_start[:60]}", file=sys.stderr)
        return content
    
    # Find start of line (include leading whitespace)
    line_start = content.rfind('\n', 0, idx)
    if line_start == -1:
        line_start = 0
    else:
        line_start += 1
    
    # Find the docstring/comment before method
    # Look for xml doc comment before
    doc_start = line_start
    # Check if there's a doc comment block before
    tmp = content[:line_start].rstrip()
    if tmp.endswith('*/') or tmp.endswith('///'):
        # Find start of comment block
        pass
    
    # Find end of method (either simple => ...; or block {})
    end_idx = idx + len(method_start)
    brace_count = 0
    in_block = False
    while end_idx < len(content):
        c = content[end_idx]
        if c == '{':
            brace_count += 1
            in_block = True
        elif c == '}':
            brace_count -= 1
            if in_block and brace_count == 0:
                end_idx += 1
                break
        elif c == ';' and not in_block:
            end_idx += 1
            break
        end_idx += 1
    
    # Include trailing newline
    while end_idx < len(content) and content[end_idx] in '\r\n':
        end_idx += 1
    
    return content[:line_start] + content[end_idx:]

def remove_doccomment_and_method(content, doc_summary_text, method_start):
    """Remove a doc comment + method."""
    # Find the doc comment that contains doc_summary_text
    # Simple approach: find the /// <summary> ... /// </summary> block before method_start
    method_idx = content.find(method_start)
    if method_idx == -1:
        print(f"WARNING: Could not find method: {method_start[:60]}", file=sys.stderr)
        return content
    
    # Find the start of the doc comment (look backwards for ///)
    search_start = content.rfind('\n', 0, method_idx)
    if search_start == -1:
        search_start = 0
    
    # Look backwards for triple-slash comment block
    doc_block_start = method_idx
    # Find /// lines before method_start
    lines_before = content[:method_idx]
    # Find the last line that is not part of a doc comment
    lines = lines_before.split('\n')
    i = len(lines) - 1
    while i >= 0 and (lines[i].strip().startswith('///') or lines[i].strip() == ''):
        i -= 1
    # i+1 is first line of doc comment (after empty lines)
    # But we want to keep empty lines before the doc comment
    # Find actual doc comment start
    j = i + 1
    while j < len(lines) and not lines[j].strip().startswith('///'):
        j += 1
    
    doc_start_pos = len('\n'.join(lines[:j])) + (1 if j > 0 else 0)
    
    # Find the end of the method
    end_idx = method_idx + len(method_start)
    brace_count = 0
    in_block = False
    while end_idx < len(content):
        c = content[end_idx]
        if c == '{':
            brace_count += 1
            in_block = True
        elif c == '}':
            brace_count -= 1
            if in_block and brace_count == 0:
                end_idx += 1
                break
        elif c == ';' and not in_block:
            end_idx += 1
            break
        end_idx += 1
    
    # Include trailing newline
    while end_idx < len(content) and content[end_idx] in '\r\n':
        end_idx += 1
    
    return content[:doc_start_pos] + content[end_idx:]

# ==================== src/Model/EnvironmentBinding.cs ====================
path = "src/Model/EnvironmentBinding.cs"
content = read(path)
content = add_attribute_before_class(content, "EnvironmentBinding")
content = content.replace(
    "public override Binding Clone() => new EnvironmentBinding {UnknownAttributes = UnknownAttributes, UnknownElements = UnknownElements, IfZeroInstallVersion = IfZeroInstallVersion, Name = Name, Value = Value, Insert = Insert, Mode = Mode, Separator = Separator, Default = Default};",
    "public override Binding Clone() => this.FastDeepClone();"
)
write(path, content)
print(f"Done: {path}")

# ==================== src/Model/Constraint.cs ====================
path = "src/Model/Constraint.cs"
content = read(path)
content = add_attribute_before_class(content, "Constraint")
content = content.replace(
    "public Constraint Clone() => new() {UnknownAttributes = UnknownAttributes, UnknownElements = UnknownElements, IfZeroInstallVersion = IfZeroInstallVersion, NotBefore = NotBefore, Before = Before};",
    "public Constraint Clone() => this.FastDeepClone();"
)
write(path, content)
print(f"Done: {path}")

# ==================== src/Model/Arg.cs ====================
path = "src/Model/Arg.cs"
content = read(path)
content = add_attribute_before_class(content, "Arg")
# Remove ICloneable<Arg> from class declaration
content = content.replace("public partial class Arg : ArgBase, ICloneable<Arg>", "public partial class Arg : ArgBase")
# Remove explicit interface implementation and its doc comment
content = content.replace(
    "    /// <summary>\n    /// Creates a deep copy of this <see cref=\"Arg\"/> instance.\n    /// </summary>\n    /// <returns>The new copy of the <see cref=\"Arg\"/>.</returns>\n    Arg ICloneable<Arg>.Clone() => new() {Value = Value};\n\n",
    ""
)
# Replace the remaining Clone method
content = content.replace(
    "public override ArgBase Clone() => ((ICloneable<Arg>)this).Clone();",
    "public override ArgBase Clone() => this.FastDeepClone();"
)
write(path, content)
print(f"Done: {path}")

# ==================== src/Model/Category.cs ====================
path = "src/Model/Category.cs"
content = read(path)
content = add_attribute_before_class(content, "Category")
content = content.replace(
    "public Category Clone() => new() {UnknownAttributes = UnknownAttributes, UnknownElements = UnknownElements, Name = Name, TypeNamespace = TypeNamespace};",
    "public Category Clone() => this.FastDeepClone();"
)
write(path, content)
print(f"Done: {path}")

# ==================== src/Model/Capabilities/DefaultProgram.cs ====================
path = "src/Model/Capabilities/DefaultProgram.cs"
content = read(path)
content = add_attribute_before_class(content, "DefaultProgram")
# Multi-line: replace from "public override Capability Clone() => new DefaultProgram" to end of block
start = "    public override Capability Clone() => new DefaultProgram\n"
new_line = "    public override Capability Clone() => this.FastDeepClone();\n"
content = replace_multiline_clone(content, start, new_line)
write(path, content)
print(f"Done: {path}")

# ==================== src/Model/Capabilities/FileTypeExtension.cs ====================
path = "src/Model/Capabilities/FileTypeExtension.cs"
content = read(path)
content = add_attribute_before_class(content, "FileTypeExtension")
content = content.replace(
    "public FileTypeExtension Clone() => new() {UnknownAttributes = UnknownAttributes, UnknownElements = UnknownElements, Value = Value, MimeType = MimeType, PerceivedType = PerceivedType};",
    "public FileTypeExtension Clone() => this.FastDeepClone();"
)
write(path, content)
print(f"Done: {path}")

# ==================== src/Model/Capabilities/FileType.cs ====================
path = "src/Model/Capabilities/FileType.cs"
content = read(path)
content = add_attribute_before_class(content, "FileType")
# Find and replace multi-line Clone
start = "    public override Capability Clone() => new FileType\n"
new_line = "    public override Capability Clone() => this.FastDeepClone();\n"
content = replace_multiline_clone(content, start, new_line)
write(path, content)
print(f"Done: {path}")

# ==================== src/Model/Capabilities/AppRegistration.cs ====================
path = "src/Model/Capabilities/AppRegistration.cs"
content = read(path)
content = add_attribute_before_class(content, "AppRegistration")
start_marker = "    public override Capability Clone()"
# Find it and check if one-liner or multi-line
idx = content.find(start_marker)
if idx != -1:
    end_of_line = content.find('\n', idx)
    line = content[idx:end_of_line]
    if '=>' in line and 'new' in line:
        # One-liner
        content = re.sub(r'public override Capability Clone\(\) => new AppRegistration \{[^}]*\};', 
                        'public override Capability Clone() => this.FastDeepClone();', content)
    else:
        # Multi-line
        start = content[idx:idx+50]
        content = replace_multiline_clone(content, "    public override Capability Clone() => new AppRegistration\n", 
                                          "    public override Capability Clone() => this.FastDeepClone();\n")
write(path, content)
print(f"Done: {path}")

# ==================== src/Model/Capabilities/AutoPlayEvent.cs ====================
path = "src/Model/Capabilities/AutoPlayEvent.cs"
content = read(path)
content = add_attribute_before_class(content, "AutoPlayEvent")
# Find the Clone line
idx = content.find("public AutoPlayEvent Clone()")
end = content.find(";", idx)
old = content[idx:end+1]
content = content.replace(old, "public AutoPlayEvent Clone() => this.FastDeepClone();")
write(path, content)
print(f"Done: {path}")

# ==================== src/Model/Capabilities/RemoveHook.cs ====================
path = "src/Model/Capabilities/RemoveHook.cs"
content = read(path)
content = add_attribute_before_class(content, "RemoveHook")
idx = content.find("public override Capability Clone()")
end = content.find(";", idx)
old = content[idx:end+1]
content = content.replace(old, "public override Capability Clone() => this.FastDeepClone();")
write(path, content)
print(f"Done: {path}")

# ==================== src/Model/Capabilities/BrowserExtension.cs ====================
path = "src/Model/Capabilities/BrowserExtension.cs"
content = read(path)
content = add_attribute_before_class(content, "BrowserExtension")
idx = content.find("public override Capability Clone()")
if idx == -1:
    idx = content.find("public BrowserExtension Clone()")
end = content.find(";", idx)
old = content[idx:end+1]
content = content.replace(old, content[idx:content.find("Clone()", idx)+7] + " => this.FastDeepClone();")
write(path, content)
print(f"Done: {path}")

# ==================== src/Model/Capabilities/UrlProtocol.cs ====================
path = "src/Model/Capabilities/UrlProtocol.cs"
content = read(path)
content = add_attribute_before_class(content, "UrlProtocol")
start = "    public override Capability Clone() => new UrlProtocol\n"
new_line = "    public override Capability Clone() => this.FastDeepClone();\n"
content = replace_multiline_clone(content, start, new_line)
write(path, content)
print(f"Done: {path}")

# ==================== src/Model/Capabilities/KnownProtocolPrefix.cs ====================
path = "src/Model/Capabilities/KnownProtocolPrefix.cs"
content = read(path)
content = add_attribute_before_class(content, "KnownProtocolPrefix")
idx = content.find("public KnownProtocolPrefix Clone()")
end = content.find(";", idx)
old = content[idx:end+1]
content = content.replace(old, "public KnownProtocolPrefix Clone() => this.FastDeepClone();")
write(path, content)
print(f"Done: {path}")

# ==================== src/Model/Capabilities/Verb.cs ====================
path = "src/Model/Capabilities/Verb.cs"
content = read(path)
content = add_attribute_before_class(content, "Verb")
start = "    public override Capability Clone() => new Verb\n"
if start not in content:
    start = "    public Verb Clone() => new()"
    end = content.find(";", content.find(start))
    old = content[content.find(start):end+1]
    content = content.replace(old, "public Verb Clone() => this.FastDeepClone();")
else:
    content = replace_multiline_clone(content, start, "    public override Capability Clone() => this.FastDeepClone();\n")
write(path, content)
print(f"Done: {path}")

# ==================== src/Model/Capabilities/Capability.cs ====================
path = "src/Model/Capabilities/Capability.cs"
content = read(path)
content = add_attribute_before_class(content, "Capability")
# abstract - keep abstract Clone() as is
write(path, content)
print(f"Done: {path}")

# ==================== src/Model/Capabilities/CapabilityList.cs ====================
path = "src/Model/Capabilities/CapabilityList.cs"
content = read(path)
content = add_attribute_before_class(content, "CapabilityList")
start = "    public CapabilityList Clone() => new()\n"
new_line = "    public CapabilityList Clone() => this.FastDeepClone();\n"
if start in content:
    content = replace_multiline_clone(content, start, new_line)
else:
    idx = content.find("public CapabilityList Clone()")
    end = content.find(";", idx)
    old = content[idx:end+1]
    content = content.replace(old, "public CapabilityList Clone() => this.FastDeepClone();")
write(path, content)
print(f"Done: {path}")

# ==================== src/Model/Capabilities/BrowserNativeMessaging.cs ====================
path = "src/Model/Capabilities/BrowserNativeMessaging.cs"
content = read(path)
content = add_attribute_before_class(content, "BrowserNativeMessaging")
idx = content.find("public override Capability Clone()")
if idx == -1:
    idx = content.find("public BrowserNativeMessaging Clone()")
line_end = content.find("\n", idx)
clone_line = content[idx:line_end]
# Check if one-liner
semicolon = content.find(";", idx)
newline = content.find("\n", idx)
if semicolon < newline:  # one-liner
    old = content[idx:semicolon+1]
    sig = "override Capability" if "override" in clone_line else "BrowserNativeMessaging"
    if "override" in clone_line:
        content = content.replace(old, "public override Capability Clone() => this.FastDeepClone();")
    else:
        content = content.replace(old, "public BrowserNativeMessaging Clone() => this.FastDeepClone();")
else:
    if "override" in clone_line:
        content = replace_multiline_clone(content, "    public override Capability Clone() => new BrowserNativeMessaging\n", 
                                          "    public override Capability Clone() => this.FastDeepClone();\n")
    else:
        content = replace_multiline_clone(content, "    public BrowserNativeMessaging Clone() => new()\n", 
                                          "    public BrowserNativeMessaging Clone() => this.FastDeepClone();\n")
write(path, content)
print(f"Done: {path}")

# ==================== src/Model/Capabilities/ContextMenu.cs ====================
path = "src/Model/Capabilities/ContextMenu.cs"
content = read(path)
content = add_attribute_before_class(content, "ContextMenu")
start = "    public override Capability Clone() => new ContextMenu\n"
new_line = "    public override Capability Clone() => this.FastDeepClone();\n"
if start in content:
    content = replace_multiline_clone(content, start, new_line)
else:
    idx = content.find("public override Capability Clone()")
    end = content.find(";", idx)
    content = content.replace(content[idx:end+1], "public override Capability Clone() => this.FastDeepClone();")
write(path, content)
print(f"Done: {path}")

# ==================== src/Model/Capabilities/ComServer.cs ====================
path = "src/Model/Capabilities/ComServer.cs"
content = read(path)
content = add_attribute_before_class(content, "ComServer")
idx = content.find("public override Capability Clone()")
semicolon = content.find(";", idx)
newline = content.find("\n", idx)
if semicolon < newline:
    old = content[idx:semicolon+1]
    content = content.replace(old, "public override Capability Clone() => this.FastDeepClone();")
else:
    content = replace_multiline_clone(content, "    public override Capability Clone() => new ComServer\n",
                                      "    public override Capability Clone() => this.FastDeepClone();\n")
write(path, content)
print(f"Done: {path}")

# ==================== src/Model/Capabilities/AutoPlay.cs ====================
path = "src/Model/Capabilities/AutoPlay.cs"
content = read(path)
content = add_attribute_before_class(content, "AutoPlay")
start = "    public override Capability Clone() => new AutoPlay\n"
new_line = "    public override Capability Clone() => this.FastDeepClone();\n"
if start in content:
    content = replace_multiline_clone(content, start, new_line)
else:
    idx = content.find("public override Capability Clone()")
    end = content.find(";", idx)
    content = content.replace(content[idx:end+1], "public override Capability Clone() => this.FastDeepClone();")
write(path, content)
print(f"Done: {path}")

# ==================== src/Model/RenameStep.cs ====================
path = "src/Model/RenameStep.cs"
content = read(path)
content = add_attribute_before_class(content, "RenameStep")
idx = content.find("public IRecipeStep Clone()")
end = content.find(";", idx)
content = content.replace(content[idx:end+1], "public IRecipeStep Clone() => this.FastDeepClone();")
write(path, content)
print(f"Done: {path}")

# ==================== src/Model/CopyFromStep.cs ====================
path = "src/Model/CopyFromStep.cs"
content = read(path)
content = add_attribute_before_class(content, "CopyFromStep")
idx = content.find("public IRecipeStep Clone()")
end = content.find(";", idx)
content = content.replace(content[idx:end+1], "public IRecipeStep Clone() => this.FastDeepClone();")
write(path, content)
print(f"Done: {path}")

# ==================== src/Model/Catalog.cs ====================
path = "src/Model/Catalog.cs"
content = read(path)
content = add_attribute_before_class(content, "Catalog")
start = "    public Catalog Clone() => new()\n"
new_line = "    public Catalog Clone() => this.FastDeepClone();\n"
content = replace_multiline_clone(content, start, new_line)
write(path, content)
print(f"Done: {path}")

# ==================== src/Model/DownloadRetrievalMethod.cs ====================
path = "src/Model/DownloadRetrievalMethod.cs"
content = read(path)
content = add_attribute_before_class(content, "DownloadRetrievalMethod")
# Keep the IRecipeStep delegation as-is
write(path, content)
print(f"Done: {path}")

# ==================== src/Model/Recipe.cs ====================
path = "src/Model/Recipe.cs"
content = read(path)
content = add_attribute_before_class(content, "Recipe")
start = "    public override RetrievalMethod Clone() => new Recipe\n"
new_line = "    public override RetrievalMethod Clone() => this.FastDeepClone();\n"
content = replace_multiline_clone(content, start, new_line)
write(path, content)
print(f"Done: {path}")

# ==================== src/Model/Selection/ImplementationSelection.cs ====================
path = "src/Model/Selection/ImplementationSelection.cs"
content = read(path)
content = add_attribute_before_class(content, "ImplementationSelection")
# Remove ICloneable<ImplementationSelection> from class declaration
content = content.replace(
    "public sealed partial class ImplementationSelection : ImplementationBase, IInterfaceUriBindingContainer, ICloneable<ImplementationSelection>, IComparable<ImplementationSelection>",
    "public sealed partial class ImplementationSelection : ImplementationBase, IInterfaceUriBindingContainer, IComparable<ImplementationSelection>"
)
# Remove the explicit interface implementation (multi-line block + doc comment)
# Find and remove from "    /// <summary>\n    /// Creates a deep copy of this <see cref="ImplementationSelection"/>"
# to the end of the method
pattern = r'    /// <summary>\n    /// Creates a deep copy of this <see cref="ImplementationSelection"/>\n.*?}\n\n'
new_content = re.sub(pattern, '', content, flags=re.DOTALL)
if new_content == content:
    print("WARNING: ImplementationSelection explicit interface not removed", file=sys.stderr)
content = new_content
# Replace public override Element Clone()
content = content.replace(
    "public override Element Clone() => ((ICloneable<ImplementationSelection>)this).Clone();",
    "public override Element Clone() => this.FastDeepClone();"
)
write(path, content)
print(f"Done: {path}")

# ==================== src/Model/Selection/Selections.cs ====================
path = "src/Model/Selection/Selections.cs"
content = read(path)
content = add_attribute_before_class(content, "Selections")
start = "    public Selections Clone() => new()\n"
new_line = "    public Selections Clone() => this.FastDeepClone();\n"
content = replace_multiline_clone(content, start, new_line)
write(path, content)
print(f"Done: {path}")

# ==================== src/Model/ExecutableInPath.cs ====================
path = "src/Model/ExecutableInPath.cs"
content = read(path)
content = add_attribute_before_class(content, "ExecutableInPath")
idx = content.find("public override Binding Clone()")
end = content.find(";", idx)
content = content.replace(content[idx:end+1], "public override Binding Clone() => this.FastDeepClone();")
write(path, content)
print(f"Done: {path}")

# ==================== src/Model/Implementation.cs ====================
path = "src/Model/Implementation.cs"
content = read(path)
content = add_attribute_before_class(content, "Implementation")
# Replace CloneImplementation() body (multi-line)
impl_start = "    public Implementation CloneImplementation()\n    {\n"
idx = content.find(impl_start)
if idx != -1:
    # Find the closing brace
    end_idx = idx + len(impl_start)
    brace_count = 1
    while end_idx < len(content) and brace_count > 0:
        if content[end_idx] == '{':
            brace_count += 1
        elif content[end_idx] == '}':
            brace_count -= 1
        end_idx += 1
    # Include trailing newline
    while end_idx < len(content) and content[end_idx] in '\r\n':
        end_idx += 1
    content = content[:idx] + "    public Implementation CloneImplementation() => this.FastDeepClone();\n\n" + content[end_idx:]
write(path, content)
print(f"Done: {path}")

# ==================== src/Model/Element.cs ====================
path = "src/Model/Element.cs"
content = read(path)
content = add_attribute_before_class(content, "Element")
# Remove the CloneFromTo method and #region/#endregion
# Keep the abstract Clone()
# Find and remove from "    protected static void CloneFromTo(Element from, Element to)" to end of method
# and also remove the #region Clone and abstract Clone if they are separated
# The #region has abstract Clone() AND CloneFromTo(). We need to keep abstract Clone() but remove CloneFromTo.
pattern = r'\n    /// <summary>\n    /// Copies all known values from one instance to another\. Helper method for instance cloning\.\n    /// </summary>\n    protected static void CloneFromTo\(Element from, Element to\).*?}\n'
new_content = re.sub(pattern, '\n', content, flags=re.DOTALL)
if new_content == content:
    print("WARNING: Element.CloneFromTo not removed", file=sys.stderr)
content = new_content
write(path, content)
print(f"Done: {path}")

# ==================== src/Model/Runner.cs ====================
path = "src/Model/Runner.cs"
content = read(path)
content = add_attribute_before_class(content, "Runner")
# Replace CloneRunner() body
runner_start = "    public Runner CloneRunner() => new()\n"
new_line = "    public Runner CloneRunner() => this.FastDeepClone();\n"
content = replace_multiline_clone(content, runner_start, new_line)
write(path, content)
print(f"Done: {path}")

# ==================== src/Model/EntryPoint.cs ====================
path = "src/Model/EntryPoint.cs"
content = read(path)
content = add_attribute_before_class(content, "EntryPoint")
start = "    public EntryPoint Clone() => new()\n"
new_line = "    public EntryPoint Clone() => this.FastDeepClone();\n"
content = replace_multiline_clone(content, start, new_line)
write(path, content)
print(f"Done: {path}")

# ==================== src/Model/GenericBinding.cs ====================
path = "src/Model/GenericBinding.cs"
content = read(path)
content = add_attribute_before_class(content, "GenericBinding")
# Find multi-line Clone
start = "    public override Binding Clone()\n"
idx = content.find(start)
if idx != -1:
    end_idx = idx + len(start)
    brace_count = 0
    in_block = False
    while end_idx < len(content):
        c = content[end_idx]
        if c == '{':
            brace_count += 1
            in_block = True
        elif c == '}':
            brace_count -= 1
            if in_block and brace_count == 0:
                end_idx += 1
                break
        elif c == ';' and not in_block:
            end_idx += 1
            break
        end_idx += 1
    while end_idx < len(content) and content[end_idx] in '\r\n':
        end_idx += 1
    content = content[:idx] + "    public override Binding Clone() => this.FastDeepClone();\n\n" + content[end_idx:]
else:
    idx = content.find("public override Binding Clone()")
    end = content.find(";", idx)
    content = content.replace(content[idx:end+1], "public override Binding Clone() => this.FastDeepClone();")
write(path, content)
print(f"Done: {path}")

# ==================== src/Model/Trust/TrustDB.cs ====================
path = "src/Model/Trust/TrustDB.cs"
content = read(path)
content = add_attribute_before_class(content, "TrustDB")
start = "    public TrustDB Clone() => new()\n"
new_line = "    public TrustDB Clone() => this.FastDeepClone();\n"
content = replace_multiline_clone(content, start, new_line)
write(path, content)
print(f"Done: {path}")

# ==================== src/Model/Trust/Key.cs ====================
path = "src/Model/Trust/Key.cs"
content = read(path)
content = add_attribute_before_class(content, "Key")
start = "    public Key Clone() => new()\n"
new_line = "    public Key Clone() => this.FastDeepClone();\n"
content = replace_multiline_clone(content, start, new_line)
write(path, content)
print(f"Done: {path}")

# ==================== src/Model/WorkingDir.cs ====================
path = "src/Model/WorkingDir.cs"
content = read(path)
content = add_attribute_before_class(content, "WorkingDir")
idx = content.find("public WorkingDir Clone()")
end = content.find(";", idx)
content = content.replace(content[idx:end+1], "public WorkingDir Clone() => this.FastDeepClone();")
write(path, content)
print(f"Done: {path}")

# ==================== src/Model/Icon.cs ====================
path = "src/Model/Icon.cs"
content = read(path)
content = add_attribute_before_class(content, "Icon")
idx = content.find("public Icon Clone()")
end = content.find(";", idx)
content = content.replace(content[idx:end+1], "public Icon Clone() => this.FastDeepClone();")
write(path, content)
print(f"Done: {path}")

# ==================== src/Model/Archive.cs ====================
path = "src/Model/Archive.cs"
content = read(path)
content = add_attribute_before_class(content, "Archive")
idx = content.find("public override RetrievalMethod Clone()")
end = content.find(";", idx)
content = content.replace(content[idx:end+1], "public override RetrievalMethod Clone() => this.FastDeepClone();")
write(path, content)
print(f"Done: {path}")

# ==================== src/Model/RetrievalMethod.cs ====================
path = "src/Model/RetrievalMethod.cs"
content = read(path)
content = add_attribute_before_class(content, "RetrievalMethod")
# abstract - keep abstract Clone() as is
write(path, content)
print(f"Done: {path}")

# ==================== src/Model/Command.cs ====================
path = "src/Model/Command.cs"
content = read(path)
content = add_attribute_before_class(content, "Command")
start = "    public Command Clone() => new()\n"
new_line = "    public Command Clone() => this.FastDeepClone();\n"
content = replace_multiline_clone(content, start, new_line)
write(path, content)
print(f"Done: {path}")

# ==================== src/Model/ForEachArgs.cs ====================
path = "src/Model/ForEachArgs.cs"
content = read(path)
content = add_attribute_before_class(content, "ForEachArgs")
# Replace CloneForEachArgs() body
fa_start = "    private ForEachArgs CloneForEachArgs() => new()\n"
new_line = "    private ForEachArgs CloneForEachArgs() => this.FastDeepClone();\n"
content = replace_multiline_clone(content, fa_start, new_line)
write(path, content)
print(f"Done: {path}")

# ==================== src/Model/FeedReference.cs ====================
path = "src/Model/FeedReference.cs"
content = read(path)
content = add_attribute_before_class(content, "FeedReference")
# Multi-line Clone using CloneFromTo
block_start = "    public FeedReference Clone()\n    {"
idx = content.find(block_start)
if idx != -1:
    end_idx = idx + len(block_start)
    brace_count = 1
    while end_idx < len(content) and brace_count > 0:
        if content[end_idx] == '{':
            brace_count += 1
        elif content[end_idx] == '}':
            brace_count -= 1
        end_idx += 1
    while end_idx < len(content) and content[end_idx] in '\r\n':
        end_idx += 1
    content = content[:idx] + "    public FeedReference Clone() => this.FastDeepClone();\n\n" + content[end_idx:]
write(path, content)
print(f"Done: {path}")

# ==================== src/Model/Preferences/FeedPreferences.cs ====================
path = "src/Model/Preferences/FeedPreferences.cs"
content = read(path)
content = add_attribute_before_class(content, "FeedPreferences")
start = "    public FeedPreferences Clone() => new()\n"
new_line = "    public FeedPreferences Clone() => this.FastDeepClone();\n"
content = replace_multiline_clone(content, start, new_line)
write(path, content)
print(f"Done: {path}")

# ==================== src/Model/Preferences/InterfacePreferences.cs ====================
path = "src/Model/Preferences/InterfacePreferences.cs"
content = read(path)
content = add_attribute_before_class(content, "InterfacePreferences")
start = "    public InterfacePreferences Clone() => new()\n"
new_line = "    public InterfacePreferences Clone() => this.FastDeepClone();\n"
content = replace_multiline_clone(content, start, new_line)
write(path, content)
print(f"Done: {path}")

# ==================== src/Model/Preferences/ImplementationPreferences.cs ====================
path = "src/Model/Preferences/ImplementationPreferences.cs"
content = read(path)
content = add_attribute_before_class(content, "ImplementationPreferences")
idx = content.find("public ImplementationPreferences Clone()")
end = content.find(";", idx)
content = content.replace(content[idx:end+1], "public ImplementationPreferences Clone() => this.FastDeepClone();")
write(path, content)
print(f"Done: {path}")

# ==================== src/Model/InterfaceReference.cs ====================
path = "src/Model/InterfaceReference.cs"
content = read(path)
content = add_attribute_before_class(content, "InterfaceReference")
idx = content.find("public InterfaceReference Clone()")
end = content.find(";", idx)
content = content.replace(content[idx:end+1], "public InterfaceReference Clone() => this.FastDeepClone();")
write(path, content)
print(f"Done: {path}")

# ==================== src/Model/OverlayBinding.cs ====================
path = "src/Model/OverlayBinding.cs"
content = read(path)
content = add_attribute_before_class(content, "OverlayBinding")
idx = content.find("public override Binding Clone()")
end = content.find(";", idx)
content = content.replace(content[idx:end+1], "public override Binding Clone() => this.FastDeepClone();")
write(path, content)
print(f"Done: {path}")

# ==================== src/Model/Binding.cs ====================
path = "src/Model/Binding.cs"
content = read(path)
content = add_attribute_before_class(content, "Binding")
# abstract - keep as is
write(path, content)
print(f"Done: {path}")

# ==================== src/Model/PackageImplementation.cs ====================
path = "src/Model/PackageImplementation.cs"
content = read(path)
content = add_attribute_before_class(content, "PackageImplementation")
# Replace CloneImplementation() body
impl_start = "    public PackageImplementation CloneImplementation()\n    {\n"
idx = content.find(impl_start)
if idx != -1:
    end_idx = idx + len(impl_start)
    brace_count = 1
    while end_idx < len(content) and brace_count > 0:
        if content[end_idx] == '{':
            brace_count += 1
        elif content[end_idx] == '}':
            brace_count -= 1
        end_idx += 1
    while end_idx < len(content) and content[end_idx] in '\r\n':
        end_idx += 1
    content = content[:idx] + "    public PackageImplementation CloneImplementation() => this.FastDeepClone();\n\n" + content[end_idx:]
write(path, content)
print(f"Done: {path}")

# ==================== src/Model/Group.cs ====================
path = "src/Model/Group.cs"
content = read(path)
content = add_attribute_before_class(content, "Group")
# Replace CloneGroup() body
group_start = "    public Group CloneGroup()\n    {\n"
idx = content.find(group_start)
if idx != -1:
    end_idx = idx + len(group_start)
    brace_count = 1
    while end_idx < len(content) and brace_count > 0:
        if content[end_idx] == '{':
            brace_count += 1
        elif content[end_idx] == '}':
            brace_count -= 1
        end_idx += 1
    while end_idx < len(content) and content[end_idx] in '\r\n':
        end_idx += 1
    content = content[:idx] + "    public Group CloneGroup() => this.FastDeepClone();\n\n" + content[end_idx:]
write(path, content)
print(f"Done: {path}")

# ==================== src/Model/RemoveStep.cs ====================
path = "src/Model/RemoveStep.cs"
content = read(path)
content = add_attribute_before_class(content, "RemoveStep")
idx = content.find("public IRecipeStep Clone()")
end = content.find(";", idx)
content = content.replace(content[idx:end+1], "public IRecipeStep Clone() => this.FastDeepClone();")
write(path, content)
print(f"Done: {path}")

# ==================== src/Model/Restriction.cs ====================
path = "src/Model/Restriction.cs"
content = read(path)
content = add_attribute_before_class(content, "Restriction")
start = "    public virtual Restriction Clone() => new()\n"
new_line = "    public virtual Restriction Clone() => this.FastDeepClone();\n"
content = replace_multiline_clone(content, start, new_line)
write(path, content)
print(f"Done: {path}")

# ==================== src/Model/Dependency.cs ====================
path = "src/Model/Dependency.cs"
content = read(path)
content = add_attribute_before_class(content, "Dependency")
# Remove ICloneable<Dependency> from class declaration
content = content.replace(
    "public partial class Dependency : Restriction, IInterfaceUriBindingContainer, ICloneable<Dependency>",
    "public partial class Dependency : Restriction, IInterfaceUriBindingContainer"
)
# Remove explicit interface method + its doc comment (multi-line)
pattern = r'    /// <summary>\n    /// Creates a deep copy of this <see cref="Dependency"/> instance\.\n    /// </summary>\n    /// <returns>The new copy of the <see cref="Dependency"/>.</returns>\n    Dependency ICloneable<Dependency>\.Clone\(\).*?};\n\n'
new_content = re.sub(pattern, '', content, flags=re.DOTALL)
if new_content == content:
    print("WARNING: Dependency explicit interface not removed", file=sys.stderr)
content = new_content
# Replace public override Restriction Clone()
content = content.replace(
    "public override Restriction Clone() => ((ICloneable<Dependency>)this).Clone();",
    "public override Restriction Clone() => this.FastDeepClone();"
)
write(path, content)
print(f"Done: {path}")

# ==================== src/Model/ExecutableInVar.cs ====================
path = "src/Model/ExecutableInVar.cs"
content = read(path)
content = add_attribute_before_class(content, "ExecutableInVar")
idx = content.find("public override Binding Clone()")
end = content.find(";", idx)
content = content.replace(content[idx:end+1], "public override Binding Clone() => this.FastDeepClone();")
write(path, content)
print(f"Done: {path}")

# ==================== src/Model/SingleFile.cs ====================
path = "src/Model/SingleFile.cs"
content = read(path)
content = add_attribute_before_class(content, "SingleFile")
idx = content.find("public override RetrievalMethod Clone()")
end = content.find(";", idx)
content = content.replace(content[idx:end+1], "public override RetrievalMethod Clone() => this.FastDeepClone();")
write(path, content)
print(f"Done: {path}")

# ==================== src/Model/Feed.cs ====================
path = "src/Model/Feed.cs"
content = read(path)
content = add_attribute_before_class(content, "Feed")
start = "    public Feed Clone() => new()\n"
new_line = "    public Feed Clone() => this.FastDeepClone();\n"
content = replace_multiline_clone(content, start, new_line)
write(path, content)
print(f"Done: {path}")

# ==================== src/Model/ArgBase.cs ====================
path = "src/Model/ArgBase.cs"
content = read(path)
content = add_attribute_before_class(content, "ArgBase")
# abstract - keep as is
write(path, content)
print(f"Done: {path}")

# ==================== src/Model/TargetBase.cs ====================
path = "src/Model/TargetBase.cs"
content = read(path)
# Remove the #region Clone / CloneFromTo / #endregion section
pattern = r'\n    #region Clone\n.*?#endregion\n'
new_content = re.sub(pattern, '\n', content, flags=re.DOTALL)
if new_content == content:
    print("WARNING: TargetBase CloneFromTo not removed", file=sys.stderr)
content = new_content
write(path, content)
print(f"Done: {path}")

# ==================== src/Model/ImplementationBase.cs ====================
path = "src/Model/ImplementationBase.cs"
content = read(path)
# Remove the #region Clone / CloneFromTo / #endregion section
pattern = r'\n    #region Clone\n.*?#endregion\n'
new_content = re.sub(pattern, '\n', content, flags=re.DOTALL)
if new_content == content:
    print("WARNING: ImplementationBase CloneFromTo not removed", file=sys.stderr)
content = new_content
write(path, content)
print(f"Done: {path}")

# ==================== src/DesktopIntegration/AccessPoints/DefaultProgram.cs ====================
path = "src/DesktopIntegration/AccessPoints/DefaultProgram.cs"
content = read(path)
content = add_attribute_before_class(content, "DefaultProgram")
idx = content.find("public override AccessPoint Clone()")
end = content.find(";", idx)
content = content.replace(content[idx:end+1], "public override AccessPoint Clone() => this.FastDeepClone();")
write(path, content)
print(f"Done: {path}")

# ==================== src/DesktopIntegration/AccessPoints/DesktopIcon.cs ====================
path = "src/DesktopIntegration/AccessPoints/DesktopIcon.cs"
content = read(path)
content = add_attribute_before_class(content, "DesktopIcon")
idx = content.find("public override AccessPoint Clone()")
end = content.find(";", idx)
content = content.replace(content[idx:end+1], "public override AccessPoint Clone() => this.FastDeepClone();")
write(path, content)
print(f"Done: {path}")

# ==================== src/DesktopIntegration/AccessPoints/FileType.cs ====================
path = "src/DesktopIntegration/AccessPoints/FileType.cs"
content = read(path)
content = add_attribute_before_class(content, "FileType")
idx = content.find("public override AccessPoint Clone()")
end = content.find(";", idx)
content = content.replace(content[idx:end+1], "public override AccessPoint Clone() => this.FastDeepClone();")
write(path, content)
print(f"Done: {path}")

# ==================== src/DesktopIntegration/AccessPoints/AccessPointList.cs ====================
path = "src/DesktopIntegration/AccessPoints/AccessPointList.cs"
content = read(path)
content = add_attribute_before_class(content, "AccessPointList")
start = "    public AccessPointList Clone() => new()\n"
new_line = "    public AccessPointList Clone() => this.FastDeepClone();\n"
content = replace_multiline_clone(content, start, new_line)
write(path, content)
print(f"Done: {path}")

# ==================== src/DesktopIntegration/AccessPoints/SendTo.cs ====================
path = "src/DesktopIntegration/AccessPoints/SendTo.cs"
content = read(path)
content = add_attribute_before_class(content, "SendTo")
idx = content.find("public override AccessPoint Clone()")
end = content.find(";", idx)
content = content.replace(content[idx:end+1], "public override AccessPoint Clone() => this.FastDeepClone();")
write(path, content)
print(f"Done: {path}")

# ==================== src/DesktopIntegration/AccessPoints/AutoStart.cs ====================
path = "src/DesktopIntegration/AccessPoints/AutoStart.cs"
content = read(path)
content = add_attribute_before_class(content, "AutoStart")
idx = content.find("public override AccessPoint Clone()")
end = content.find(";", idx)
content = content.replace(content[idx:end+1], "public override AccessPoint Clone() => this.FastDeepClone();")
write(path, content)
print(f"Done: {path}")

# ==================== src/DesktopIntegration/AccessPoints/UrlProtocol.cs ====================
path = "src/DesktopIntegration/AccessPoints/UrlProtocol.cs"
content = read(path)
content = add_attribute_before_class(content, "UrlProtocol")
idx = content.find("public override AccessPoint Clone()")
end = content.find(";", idx)
content = content.replace(content[idx:end+1], "public override AccessPoint Clone() => this.FastDeepClone();")
write(path, content)
print(f"Done: {path}")

# ==================== src/DesktopIntegration/AccessPoints/CapabilitiyRegistration.cs ====================
path = "src/DesktopIntegration/AccessPoints/CapabilitiyRegistration.cs"
content = read(path)
content = add_attribute_before_class(content, "CapabilityRegistration")
idx = content.find("public override AccessPoint Clone()")
end = content.find(";", idx)
content = content.replace(content[idx:end+1], "public override AccessPoint Clone() => this.FastDeepClone();")
write(path, content)
print(f"Done: {path}")

# ==================== src/DesktopIntegration/AccessPoints/QuickLaunch.cs ====================
path = "src/DesktopIntegration/AccessPoints/QuickLaunch.cs"
content = read(path)
content = add_attribute_before_class(content, "QuickLaunch")
idx = content.find("public override AccessPoint Clone()")
end = content.find(";", idx)
content = content.replace(content[idx:end+1], "public override AccessPoint Clone() => this.FastDeepClone();")
write(path, content)
print(f"Done: {path}")

# ==================== src/DesktopIntegration/AccessPoints/MenuEntry.cs ====================
path = "src/DesktopIntegration/AccessPoints/MenuEntry.cs"
content = read(path)
content = add_attribute_before_class(content, "MenuEntry")
idx = content.find("public override AccessPoint Clone()")
end = content.find(";", idx)
content = content.replace(content[idx:end+1], "public override AccessPoint Clone() => this.FastDeepClone();")
write(path, content)
print(f"Done: {path}")

# ==================== src/DesktopIntegration/AccessPoints/AppAlias.cs ====================
path = "src/DesktopIntegration/AccessPoints/AppAlias.cs"
content = read(path)
content = add_attribute_before_class(content, "AppAlias")
idx = content.find("public override AccessPoint Clone()")
end = content.find(";", idx)
content = content.replace(content[idx:end+1], "public override AccessPoint Clone() => this.FastDeepClone();")
write(path, content)
print(f"Done: {path}")

# ==================== src/DesktopIntegration/AccessPoints/ContextMenu.cs ====================
path = "src/DesktopIntegration/AccessPoints/ContextMenu.cs"
content = read(path)
content = add_attribute_before_class(content, "ContextMenu")
idx = content.find("public override AccessPoint Clone()")
end = content.find(";", idx)
content = content.replace(content[idx:end+1], "public override AccessPoint Clone() => this.FastDeepClone();")
write(path, content)
print(f"Done: {path}")

# ==================== src/DesktopIntegration/AccessPoints/AutoPlay.cs ====================
path = "src/DesktopIntegration/AccessPoints/AutoPlay.cs"
content = read(path)
content = add_attribute_before_class(content, "AutoPlay")
idx = content.find("public override AccessPoint Clone()")
end = content.find(";", idx)
content = content.replace(content[idx:end+1], "public override AccessPoint Clone() => this.FastDeepClone();")
write(path, content)
print(f"Done: {path}")

# ==================== src/DesktopIntegration/AccessPoints/AccessPoint.cs ====================
path = "src/DesktopIntegration/AccessPoints/AccessPoint.cs"
content = read(path)
content = add_attribute_before_class(content, "AccessPoint")
# abstract - keep as is
write(path, content)
print(f"Done: {path}")

# ==================== src/DesktopIntegration/AccessPoints/MockAccessPoint.cs ====================
path = "src/DesktopIntegration/AccessPoints/MockAccessPoint.cs"
content = read(path)
content = add_attribute_before_class(content, "MockAccessPoint")
# Find Clone start
idx = content.find("public override AccessPoint Clone()")
end_line = content.find("\n", idx)
clone_line = content[idx:end_line]
if "=>" in clone_line:
    end = content.find(";", idx)
    content = content.replace(content[idx:end+1], "public override AccessPoint Clone() => this.FastDeepClone();")
else:
    # Multi-line
    content = replace_multiline_clone(content, "    public override AccessPoint Clone() => new MockAccessPoint\n",
                                      "    public override AccessPoint Clone() => this.FastDeepClone();\n")
    # Check if it's a block form
    if "public override AccessPoint Clone()\n" in content:
        start_blk = "    public override AccessPoint Clone()\n    {\n"
        idx2 = content.find(start_blk)
        if idx2 != -1:
            end_idx = idx2 + len(start_blk)
            brace_count = 1
            while end_idx < len(content) and brace_count > 0:
                if content[end_idx] == '{':
                    brace_count += 1
                elif content[end_idx] == '}':
                    brace_count -= 1
                end_idx += 1
            while end_idx < len(content) and content[end_idx] in '\r\n':
                end_idx += 1
            content = content[:idx2] + "    public override AccessPoint Clone() => this.FastDeepClone();\n\n" + content[end_idx:]
write(path, content)
print(f"Done: {path}")

# ==================== src/DesktopIntegration/AppEntry.cs ====================
path = "src/DesktopIntegration/AppEntry.cs"
content = read(path)
content = add_attribute_before_class(content, "AppEntry")
start = "    public AppEntry Clone() => new()\n"
new_line = "    public AppEntry Clone() => this.FastDeepClone();\n"
content = replace_multiline_clone(content, start, new_line)
write(path, content)
print(f"Done: {path}")

# ==================== src/DesktopIntegration/AppList.cs ====================
path = "src/DesktopIntegration/AppList.cs"
content = read(path)
content = add_attribute_before_class(content, "AppList")
start = "    public AppList Clone() => new()\n"
new_line = "    public AppList Clone() => this.FastDeepClone();\n"
content = replace_multiline_clone(content, start, new_line)
write(path, content)
print(f"Done: {path}")

print("\nAll done!")
