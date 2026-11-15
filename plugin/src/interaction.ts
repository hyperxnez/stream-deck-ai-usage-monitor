export class PressTracker {
 private pressed=new Map<string,number>();
 down(id:string,now:number):void {if(!this.pressed.has(id))this.pressed.set(id,now);}
 up(id:string,now:number):'short'|'hold'|'none' {const started=this.pressed.get(id);this.pressed.delete(id);return started===undefined?'none':now-started>=700?'hold':'short';}
 remove(id:string):void {this.pressed.delete(id);}
}
