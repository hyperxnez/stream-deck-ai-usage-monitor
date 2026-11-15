export interface Bucket {groupId:string;groupName:string;window:string;remainingPercent:number|null;resetAt:string|null}
export interface Provider {provider:string;state:string;accountLabel:string|null;buckets:Bucket[];observedAt:string|null;lastAttemptAt:string|null;errorCode:string|null;retryAt:string|null;source:string|null}
export interface Snapshot {version:number;generatedAt:string;providers:Provider[]}
export type Settings = {provider?:string;window?:string;group?:string;compact?:boolean};
export function selectBucket(snapshot:Snapshot|null,settings:Settings):Bucket|undefined {
 const p=snapshot?.providers.find(p=>p.provider===(settings.provider??'claude'));
 const group=settings.group||p?.buckets.find(b=>b.groupId==='default')?.groupId||p?.buckets[0]?.groupId;
 return p?.buckets.find(b=>b.groupId===group&&b.window===(settings.window??'fiveHour'));
}
function escape(value:string):string{return value.replace(/[<>&"']/g,c=>({'<':'&lt;','>':'&gt;','&':'&amp;','"':'&quot;',"'":'&apos;'}[c]!));}
export function renderKey(snapshot:Snapshot|null,settings:Settings,now:number,countdown=false):string {
 const provider=settings.provider??'claude';const p=snapshot?.providers.find(p=>p.provider===provider);const b=selectBucket(snapshot,settings);
 const title=provider==='antigravity'?'ANTI':provider.toUpperCase();const window=settings.window==='sevenDay'?'7D':'5H';
 const age=p?.observedAt?Math.max(0,now-Date.parse(p.observedAt)):Infinity;
 const reset=b?.resetAt?Date.parse(b.resetAt):NaN;const pending=Number.isFinite(reset)&&reset<=now;
 const hasPercent=b?.remainingPercent!=null&&Number.isFinite(b.remainingPercent);
 let label='LIVE';let value=hasPercent?Math.round(b!.remainingPercent!)+'%':'N/A';
 let color=hasPercent?(b!.remainingPercent!<10?'#ff686b':b!.remainingPercent!<=30?'#ffc469':'#66dfb3'):'#92a0b4';
 if(!snapshot||!p){label='OFFLINE';value='--';color='#8591a6';}
 else if(p.state==='needsLogin'){label='LOGIN';value='--';color='#ffc469';}
 else if(p.state==='accountChanged'){label='ACCOUNT';value='--';color='#ffc469';}
 else if(pending||(p.state==='resetPending'&&b&&!Number.isFinite(reset))){label='RESET';value='...';color='#8fb6ff';}
 else if(p.state==='rateLimited'){label='WAIT';color='#ffc469';}
 else if(p.state==='stale'||age>240000){label='OLD';color='#ffc469';}
 else if(p.state!=='fresh'&&p.state!=='resetPending'){label='N/A';value='--';color='#8591a6';}
 else if(!b){label='N/A';}
 if(countdown&&b&&Number.isFinite(reset)&&reset>now&&p?.state!=='needsLogin'&&p?.state!=='accountChanged'){
  const mins=Math.ceil((reset-now)/60000);value=mins>=1440?`${Math.floor(mins/1440)}d ${Math.floor(mins%1440/60)}h`:mins>=60?`${Math.floor(mins/60)}h ${mins%60}m`:`${mins}m`;
 }
 const group=provider==='antigravity'?(b?.groupName??'Select group').replace('Gemini Models','GEMINI').replace('Claude and GPT models','CLAUDE/GPT').slice(0,18):'REMAINING';
 const bottom=label==='LIVE'&&Number.isFinite(reset)?new Date(reset).toLocaleTimeString('en-GB',{hour:'2-digit',minute:'2-digit'}):label;
 const percent=hasPercent?Math.min(100,Math.max(0,b!.remainingPercent!)):0;
 return `<svg xmlns="http://www.w3.org/2000/svg" width="144" height="144" viewBox="0 0 144 144"><rect width="144" height="144" rx="16" fill="#111723"/><rect x="5" y="5" width="134" height="134" rx="12" fill="none" stroke="${color}" stroke-width="2" opacity=".5"/><text x="14" y="27" fill="#dbe3ee" font-family="Segoe UI,Arial,sans-serif" font-size="15" font-weight="700">${title}</text><text x="130" y="27" text-anchor="end" fill="${color}" font-family="Segoe UI,Arial,sans-serif" font-size="14" font-weight="700">${window}</text><text x="72" y="78" text-anchor="middle" fill="${color}" font-family="Segoe UI,Arial,sans-serif" font-size="${value.length>5?29:43}" font-weight="700">${escape(value)}</text><text x="72" y="99" text-anchor="middle" fill="#a3afc1" font-family="Segoe UI,Arial,sans-serif" font-size="11">${escape(group)}</text><rect x="14" y="110" width="116" height="5" rx="2.5" fill="#2a3344"/><rect x="14" y="110" width="${116*percent/100}" height="5" rx="2.5" fill="${color}"/><text x="72" y="132" text-anchor="middle" fill="${color}" font-family="Segoe UI,Arial,sans-serif" font-size="12" font-weight="600">${escape(bottom)}</text></svg>`;
}
