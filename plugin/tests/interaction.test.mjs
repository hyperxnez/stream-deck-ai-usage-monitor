import test from 'node:test';import assert from 'node:assert/strict';import {PressTracker} from '../dist/interaction.js';
test('700 ms press refreshes once and is not also a short press',()=>{const t=new PressTracker();t.down('a',1000);assert.equal(t.up('a',1700),'hold');assert.equal(t.up('a',1800),'none');});
test('short press shows countdown',()=>{const t=new PressTracker();t.down('a',1000);assert.equal(t.up('a',1699),'short');});
test('disappearing action cancels pressed state',()=>{const t=new PressTracker();t.down('a',1000);t.remove('a');assert.equal(t.up('a',2000),'none');});
