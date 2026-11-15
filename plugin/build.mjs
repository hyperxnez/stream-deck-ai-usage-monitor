import {build} from 'esbuild';
import {mkdir,writeFile,rm} from 'node:fs/promises';
import {deflateSync} from 'node:zlib';
await mkdir('dist',{recursive:true});
await build({entryPoints:['src/render.ts','src/interaction.ts','src/frames.ts'],outdir:'dist',bundle:true,platform:'node',format:'esm',target:'node20',sourcemap:false});
if(process.argv.includes('--tests'))process.exit(0);
const {renderKey}=await import('./dist/render.js');
const assetDir='com.local.aiusagedeck.sdPlugin/imgs';await mkdir(assetDir,{recursive:true});
for(const [id,provider,window] of [['claude5','claude','fiveHour'],['claude7','claude','sevenDay'],['codex5','codex','fiveHour'],['codex7','codex','sevenDay'],['anti5','antigravity','fiveHour'],['anti7','antigravity','sevenDay']])await writeFile(`${assetDir}/${id}.svg`,renderKey(null,{provider,window},Date.now()));
await rm(`${assetDir}/plugin.svg`,{force:true});
await writeFile(`${assetDir}/category.svg`,'<svg xmlns="http://www.w3.org/2000/svg" width="144" height="144"><rect width="144" height="144" rx="24" fill="#111723"/><path d="M31 103V67h17v36zm33 0V43h17v60zm33 0V24h17v79z" fill="#66dfb3"/></svg>');
// Manifest plugin icons require PNG; draw the small geometric mark without a graphics dependency.
function crc32(data){let n=0xffffffff;for(const byte of data){n^=byte;for(let i=0;i<8;i++)n=(n>>>1)^((n&1)?0xedb88320:0);}return(n^0xffffffff)>>>0;}
function chunk(name,data){const type=Buffer.from(name);const size=Buffer.alloc(4);size.writeUInt32BE(data.length);const crc=Buffer.alloc(4);crc.writeUInt32BE(crc32(Buffer.concat([type,data])));return Buffer.concat([size,type,data,crc]);}
for(const width of [256,512]){
 const rows=Buffer.alloc((width*4+1)*width);
 for(let y=0;y<width;y++)for(let x=0;x<width;x++){const offset=y*(width*4+1)+1+x*4;const px=x*256/width,py=y*256/width;const bar=(px>=55&&px<85&&py>=119&&py<184)||(px>=114&&px<144&&py>=76&&py<184)||(px>=172&&px<202&&py>=43&&py<184);rows.set(bar?[102,223,179,255]:[17,23,35,255],offset);}
 const ihdr=Buffer.alloc(13);ihdr.writeUInt32BE(width,0);ihdr.writeUInt32BE(width,4);ihdr[8]=8;ihdr[9]=6;
 await writeFile(`${assetDir}/plugin${width===512?'@2x':''}.png`,Buffer.concat([Buffer.from([137,80,78,71,13,10,26,10]),chunk('IHDR',ihdr),chunk('IDAT',deflateSync(rows)),chunk('IEND',Buffer.alloc(0))]));
}
await build({entryPoints:['src/plugin.ts'],outfile:'com.local.aiusagedeck.sdPlugin/bin/plugin.js',bundle:true,platform:'node',format:'esm',target:'node20',sourcemap:false,banner:{js:'import {createRequire as __createRequire} from "node:module";const require=__createRequire(import.meta.url);'}});
