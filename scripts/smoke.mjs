import {spawn} from 'node:child_process';import net from 'node:net';import {setTimeout as delay} from 'node:timers/promises';
const executable=process.argv[2];if(!executable)throw new Error('collector path required');
const owned=spawn(executable,[],{windowsHide:true,stdio:['ignore','ignore','pipe']});owned.stderr.resume();
const pipe='\\\\.\\pipe\\AIUsageDeck.v1';
function request(method){return new Promise((resolve,reject)=>{const s=net.createConnection(pipe);let text='';s.setTimeout(5000,()=>s.destroy(new Error('timeout')));s.once('connect',()=>s.write(JSON.stringify({version:1,id:'smoke',method})+'\n'));s.setEncoding('utf8');s.on('data',chunk=>{text+=chunk;if(text.includes('\n')){s.destroy();try{resolve(JSON.parse(text.split('\n')[0]));}catch(e){reject(e);}}});s.on('error',reject);});}
try{
 let snapshot;for(let i=0;i<50;i++){try{snapshot=(await request('get')).snapshot;if(snapshot.providers.every(p=>p.state==='fresh'))break;}catch{}await delay(1000);}
 if(!snapshot||snapshot.version!==1)throw new Error('no snapshot');
 if(snapshot.providers.length!==3)throw new Error('missing provider');
 const duplicate=spawn(executable,[],{windowsHide:true,stdio:'ignore'});const duplicateExit=await new Promise(resolve=>{duplicate.once('exit',resolve);setTimeout(()=>{duplicate.kill();resolve(-1);},10000).unref();});
 if(duplicateExit!==0)throw new Error('duplicate instance was not excluded');
 const json=JSON.stringify(snapshot);for(const forbidden of ['accessToken','refreshToken','email','organizationUuid','Bearer'])if(json.includes(forbidden))throw new Error('sensitive field in snapshot');
 console.log(JSON.stringify({smoke:'pass',duplicateExcluded:true,providers:snapshot.providers.map(p=>({provider:p.provider,state:p.state,windows:p.buckets.map(b=>b.window)})),payloadBytes:json.length}));
 if(!snapshot.providers.every(p=>p.state==='fresh'))process.exitCode=2;
}finally{owned.kill();}
