"""Заглушки замість XAML-компілятора: поля x:Name, InitializeComponent, перевірка обробників подій і шляхів x:Bind"""
import re, sys, glob, os
import xml.etree.ElementTree as ET

ROOT = sys.argv[1]; OUT = sys.argv[2]
XNS = 'http://schemas.microsoft.com/winfx/2006/xaml'
PRES = 'http://schemas.microsoft.com/winfx/2006/xaml/presentation'
SHAPES = {'Rectangle', 'Ellipse', 'Line', 'Polyline', 'Path'}
MEDIA = {'SolidColorBrush'}
EVENTS = {'Click', 'Toggled', 'SelectionChanged', 'ValueChanged', 'ItemClick', 'DragItemsCompleted', 'TextChanged',
          'Loaded', 'Unloaded', 'KeyDown', 'PasswordChanged', 'Checked', 'Unchecked'}

def ctype(tag):
    ns, name = tag[1:].split('}') if tag.startswith('{') else ('', tag)
    if ns == PRES:
        if name in SHAPES: return 'Microsoft.UI.Xaml.Shapes.' + name
        if name in MEDIA: return 'Microsoft.UI.Xaml.Media.' + name
        if name in ('Page', 'Window', 'Application'): return 'Microsoft.UI.Xaml.' + ('Controls.Page' if name == 'Page' else name)
        return 'Microsoft.UI.Xaml.Controls.' + name
    if ns.startswith('using:'): return ns[6:] + '.' + name
    return None

for path in glob.glob(os.path.join(ROOT, '**', '*.xaml'), recursive=True):
    if '/obj/' in path or '/bin/' in path or '/tests/' in path: continue
    text = open(path, encoding='utf-8').read()
    root = ET.fromstring(text)
    cls = root.get('{%s}Class' % XNS)
    if not cls: continue
    ns, name = cls.rsplit('.', 1)
    base = ctype(root.tag)
    fields, checks = [], []
    counter = [0]

    def walk(el, datatype):
        t = ctype(el.tag)
        dt = el.get('{%s}DataType' % XNS)
        if dt:
            prefix, typ = dt.split(':')
            nsuri = [v for k, v in re.findall(r'xmlns:(\w+)="([^"]+)"', text) if k == prefix][0]
            datatype = nsuri[6:] + '.' + typ
        xname = el.get('{%s}Name' % XNS)
        if xname and t and datatype is None:
            fields.append(f'    internal {t} {xname} = null!;')
        for attr, val in el.attrib.items():
            a = attr.split('}')[-1]
            if a in EVENTS and re.fullmatch(r'\w+', val) and t:
                counter[0] += 1
                checks.append(f'        {{ {t} e{counter[0]} = null!; e{counter[0]}.{a} += {val}; }}')
            m = re.fullmatch(r'\{x:Bind\s+([^,}]+)(,.*)?\}', val)
            if m:
                expr = m.group(1).strip()
                src = f'(({datatype})null!)' if datatype else 'this'
                fm = re.fullmatch(r'(\w+):([\w.]+)\((.*)\)', expr)
                if fm:
                    pre, fn, args = fm.groups()
                    nsuri = [v for k, v in re.findall(r'xmlns:(\w+)="([^"]+)"', text) if k == pre][0]
                    argexpr = ', '.join(f'{src}.{x.strip()}' for x in args.split(',') if x.strip())
                    checks.append(f'        _ = {nsuri[6:]}.{fn}({argexpr});')
                else:
                    checks.append(f'        _ = {src}.{expr};')
        for child in el:
            walk(child, datatype)

    walk(root, None)
    out = [f'namespace {ns};', '#pragma warning disable CS0169, CS0649, CS8321',
           f'partial class {name}', '{'] + fields + [
           '    private void InitializeComponent() { }',
           '    private void __CheckXaml()', '    {'] + checks + ['    }', '}']
    open(os.path.join(OUT, name + '.g.cs'), 'w').write('\n'.join(out) + '\n')
    print('stub', cls, len(fields), 'fields', len(checks), 'checks')
