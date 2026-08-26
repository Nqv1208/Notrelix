#!/usr/bin/env node
import { createHash } from 'node:crypto';
import { readFileSync } from 'node:fs';
import { isAbsolute, normalize, resolve, sep } from 'node:path';
import { spawnSync } from 'node:child_process';
function arg(name){const i=process.argv.indexOf(name);if(i<0||i+1>=process.argv.length)throw new Error(`missing ${name}`);return process.argv[i+1]}
function safe(v){if(!v||isAbsolute(v))return false;const n=normalize(v);return n!=='..'&&!n.startsWith(`..${sep}`)}
const component=arg('--component'), archive=resolve(arg('--archive')), manifestPath=resolve(arg('--manifest')), destination=resolve(arg('--destination'));const m=JSON.parse(readFileSync(manifestPath,'utf8'));
if(m.api_version!=='delivery.notrelix.dev/v1'||m.kind!=='FrontendHostArtifact'||m.component_id!==component)throw new Error('artifact manifest identity mismatch');
const digest=createHash('sha256').update(readFileSync(archive)).digest('hex');if(digest!==m.sha256)throw new Error('artifact hash mismatch');
const listing=spawnSync('tar',['-tzf',archive],{encoding:'utf8'});if(listing.status!==0)process.exit(listing.status??1);const allowed=m.paths.map(p=>normalize(p).replaceAll('\\','/').replace(/\/+$/,''));
for(const raw of listing.stdout.split(/\r?\n/).filter(Boolean)){const member=raw.replaceAll('\\','/').replace(/\/+$/,'');if(!safe(member)||!allowed.some(p=>member===p||member.startsWith(`${p}/`)))throw new Error(`unsafe/out-of-contract tar member ${raw}`)}
const ex=spawnSync('tar',['-xzf',archive,'-C',destination],{stdio:'inherit'});if(ex.status!==0)process.exit(ex.status??1);
