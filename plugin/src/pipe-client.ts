import net from 'node:net';
import {spawn} from 'node:child_process';
import {fileURLToPath} from 'node:url';
import {Frames} from './frames.js';
import type {Snapshot} from './render.js';
const pipe='\\\\.\\pipe\\AIUsageDeck.v1';
export class PipeClient {
 private socket:net.Socket|null=null;private retry:NodeJS.Timeout|undefined;private lastLaunch=0;private received=0;
 constructor(private update:(s:Snapshot|null)=>void){this.connect();setInterval(()=>{if(this.received&&Date.now()-this.received>8000){this.update(null);this.socket?.destroy();}},2000).unref();}
 private ensureCollector():void{
  if(Date.now()-this.lastLaunch<30000)return;this.lastLaunch=Date.now();
  const executable=fileURLToPath(new URL('../collector/AIUsageDeck.Collector.exe',import.meta.url));
  const child=spawn(executable,[],{windowsHide:true,detached:true,stdio:'ignore'});child.on('error',()=>{});child.unref();
 }
 private connect():void{
  const socket=net.createConnection(pipe);this.socket=socket;const frames=new Frames();socket.setEncoding('utf8');
  socket.once('connect',()=>{socket.write(JSON.stringify({version:1,id:'watch',method:'subscribe'})+'\n');});
  socket.on('data',(chunk:string)=>{try{for(const value of frames.push(chunk)){
   const message=value as {version?:number;snapshot?:Snapshot};
   if(message.version!==1||message.snapshot?.version!==1||!Array.isArray(message.snapshot.providers))throw new Error('protocol');
   this.received=Date.now();this.update(message.snapshot);
  }}catch{socket.destroy();}});
  socket.on('error',()=>{this.ensureCollector();});
  socket.once('close',()=>{this.socket=null;this.received=0;this.update(null);clearTimeout(this.retry);this.retry=setTimeout(()=>this.connect(),3000);});
 }
 refresh(provider:string):void{
  if(!['claude','codex','antigravity'].includes(provider))return;
  const socket=net.createConnection(pipe);socket.setTimeout(45000,()=>socket.destroy());
  socket.once('connect',()=>socket.write(JSON.stringify({version:1,id:'refresh',method:'refresh',provider})+'\n'));
  socket.once('data',()=>socket.destroy());socket.on('error',()=>socket.destroy());
 }
}
