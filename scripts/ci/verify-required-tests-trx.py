#!/usr/bin/env python3
import sys,xml.etree.ElementTree as ET
if len(sys.argv)<3:raise SystemExit('usage: verify-required-tests-trx.py FILE TEST...')
root=ET.parse(sys.argv[1]).getroot();names={n.attrib.get('testName','') for n in root.iter() if n.tag.endswith('UnitTestResult')};missing=[x for x in sys.argv[2:] if not any(n==x or n.startswith(x+'.') for n in names)]
if missing:raise SystemExit('required tests did not execute: '+', '.join(missing))
print('required test execution PASS')
