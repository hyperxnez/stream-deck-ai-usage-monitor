import streamDeck,{action,SingletonAction,type KeyAction,type WillAppearEvent,type WillDisappearEvent,type KeyDownEvent,type KeyUpEvent,type DidReceiveSettingsEvent,type SendToPluginEvent} from '@elgato/streamdeck';
import {PipeClient} from './pipe-client.js';
import {renderKey,type Settings,type Snapshot} from './render.js';
import {PressTracker} from './interaction.js';
let snapshot:Snapshot|null=null;
const pressed=new PressTracker();
type Visible={action:KeyAction<Settings>;settings:Settings;countdownUntil:number;image?:string};
const visible=new Map<string,Visible>();
const client=new PipeClient(s=>{snapshot=s;paint();});
function normalize(settings:Settings,defaults:Settings):Settings{
 return {provider:['claude','codex','antigravity'].includes(settings.provider??'')?settings.provider:defaults.provider,
 window:['fiveHour','sevenDay'].includes(settings.window??'')?settings.window:defaults.window,
 group:typeof settings.group==='string'?settings.group.slice(0,100):''};
}
function paint():void{
 for(const item of visible.values()){
  const svg=renderKey(snapshot,item.settings,Date.now(),item.countdownUntil>Date.now());
  if(svg===item.image)continue;item.image=svg;void item.action.setImage('data:image/svg+xml;base64,'+Buffer.from(svg).toString('base64')).catch(()=>{});
 }
}
class UsageAction extends SingletonAction<Settings>{
 constructor(private defaults:Settings){super();}
 override async onWillAppear(ev:WillAppearEvent<Settings>){
  if(!ev.action.isKey())return;const settings=normalize(ev.payload.settings,this.defaults);
  visible.set(ev.action.id,{action:ev.action,settings,countdownUntil:0});
  if(JSON.stringify(ev.payload.settings)!==JSON.stringify(settings))await ev.action.setSettings(settings);
  await ev.action.setTitle('');paint();
 }
 override onWillDisappear(ev:WillDisappearEvent<Settings>){visible.delete(ev.action.id);pressed.remove(ev.action.id);}
 override onDidReceiveSettings(ev:DidReceiveSettingsEvent<Settings>){const item=visible.get(ev.action.id);if(item){item.settings=normalize(ev.payload.settings,this.defaults);paint();}}
 override onKeyDown(ev:KeyDownEvent<Settings>){pressed.down(ev.action.id,Date.now());}
 override onKeyUp(ev:KeyUpEvent<Settings>){const type=pressed.up(ev.action.id,Date.now());const item=visible.get(ev.action.id);if(!item)return;
  if(type==='hold')client.refresh(item.settings.provider??'claude');else if(type==='short')item.countdownUntil=Date.now()+5000;paint();
 }
 override async onSendToPlugin(ev:SendToPluginEvent<unknown & Record<string,never>,Settings>){
  const settings=visible.get(ev.action.id)?.settings??this.defaults;const provider=snapshot?.providers.find(p=>p.provider===settings.provider);
  const groups=[...new Map((provider?.buckets??[]).map(b=>[b.groupId,{id:b.groupId,name:b.groupName}])).values()];
  await streamDeck.ui.sendToPropertyInspector({groups,state:provider?.state??'offline',observedAt:provider?.observedAt??null,errorCode:provider?.errorCode??null});
 }
}
@action({UUID:'com.local.aiusagedeck.claude5'})class Claude5 extends UsageAction{constructor(){super({provider:'claude',window:'fiveHour'});}}
@action({UUID:'com.local.aiusagedeck.claude7'})class Claude7 extends UsageAction{constructor(){super({provider:'claude',window:'sevenDay'});}}
@action({UUID:'com.local.aiusagedeck.codex5'})class Codex5 extends UsageAction{constructor(){super({provider:'codex',window:'fiveHour'});}}
@action({UUID:'com.local.aiusagedeck.codex7'})class Codex7 extends UsageAction{constructor(){super({provider:'codex',window:'sevenDay'});}}
@action({UUID:'com.local.aiusagedeck.anti5'})class Anti5 extends UsageAction{constructor(){super({provider:'antigravity',window:'fiveHour'});}}
@action({UUID:'com.local.aiusagedeck.anti7'})class Anti7 extends UsageAction{constructor(){super({provider:'antigravity',window:'sevenDay'});}}
for(const a of [new Claude5(),new Claude7(),new Codex5(),new Codex7(),new Anti5(),new Anti7()])streamDeck.actions.registerAction(a);
setInterval(paint,1000).unref();
void streamDeck.connect();
