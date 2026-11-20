const $ = id => document.getElementById(id);
const names = ['Tide Steps', 'Brass Arcade', 'Citrus Court', 'Sea Lanterns', 'The Sun Window', 'Kingfisher Crown'];
const stories = [
  'Salt has silvered the glass along the landing. Let the morning light back in.',
  'The covered arcade once shone amber above the promenade.',
  'In the citrus court, the old panes scatter sunlight over the orange trees.',
  'Glass lanterns line the sea wall. Bring their colours back before dusk.',
  'The west window once cast a golden circle on the tiled floor.',
  'High in the cupola, a kingfisher waits for its last pane of blue.'
];
const pieces = ['', 'lagoon', 'honey', 'coral', 'orchid', 'fern'];
const cells = [];
let master = false;
function storageKey() { return master ? 'mosaic-pier-master-v1' : 'mosaic-pier-v1'; }
function startBoard(ch) { return master ? rules.mp_start_master(ch) : rules.mp_start(ch); }
let rules, selected = -1, hinted = [], busy = false, audio, sound = false;
let save = {chapter:0, restored:0, history:[]};
let pointer, skipClick = false;
const sleep = ms => new Promise(r => setTimeout(r, matchMedia('(prefers-reduced-motion: reduce)').matches ? 0 : ms));
function persist() { try { localStorage.setItem(storageKey(), JSON.stringify(save)); } catch {} }
function note(message) { $('message').textContent = message; }
function ring(tone) {
  if (!sound || !audio) return;
  const oscillator = audio.createOscillator(), gain = audio.createGain(), now = audio.currentTime;
  oscillator.type = 'sine'; oscillator.frequency.value = tone;
  gain.gain.setValueAtTime(.001, now); gain.gain.exponentialRampToValueAtTime(.075, now+.018);
  gain.gain.exponentialRampToValueAtTime(.001, now+.34);
  oscillator.connect(gain).connect(audio.destination); oscillator.start(now); oscillator.stop(now+.35);
}
function modal(tag, title, copy, label, act, cancel = false) {
  const d = $('dialog');
  const plate=$('plate');plate.hidden=!(tag==='GLASSWORK COMPLETE'||tag==='SIX SECTIONS RESTORED');
  plate.style.backgroundPosition=['18% 85%','15% 55%','80% 74%','65% 40%','45% 25%','70% 10%'][save.chapter];
  $('modalTag').textContent=tag; $('modalTitle').textContent=title; $('modalText').textContent=copy;
  $('modalAction').textContent=label;
  $('modalAction').onclick=()=>{d.close();act?.();};
  $('modalCancel').textContent='Keep playing';$('modalCancel').hidden=!cancel; $('modalCancel').onclick=()=>d.close();
  d.oncancel=e=>{if(!cancel && rules?.mp_status()>=2)e.preventDefault();};
  if (!d.open) d.showModal();
}
function journal() {
  $('chapter').textContent=names[save.chapter];
  $('chapterNo').textContent=(master?'MASTERWORK ':'CHAPTER ')+String(save.chapter+1).padStart(2,'0')+' / 06';
  $('story').textContent=stories[save.chapter]; $('progress').textContent=save.restored+' of 6 sections restored';
  $('steps').replaceChildren(...names.map((name,i)=>{const s=document.createElement('i');s.title=name;s.classList.toggle('done',i<save.restored);return s;}));
  $('color').style.clipPath='inset(0 '+(100-100*save.restored/6)+'% 0 0)';
}
function paint() {
  for (const [i,cell] of cells.entries()) {
    const type=pieces[rules.mp_tile(i)], img=cell.firstChild, frost=!!rules.mp_frost(i);
    const src='assets/'+type+'.png';
    if(img.getAttribute('src')!==src) img.src=src;
    cell.classList.toggle('double-frost',rules.mp_frost(i)===2);
    cell.classList.toggle('frost',frost);cell.classList.toggle('selected',i===selected);cell.classList.toggle('hinted',hinted.includes(i));
    cell.setAttribute('aria-pressed',String(i===selected));
    cell.setAttribute('aria-label','Row '+(1+Math.floor(i/6))+', column '+(i%6+1)+', '+type+(rules.mp_frost(i)===2?', double frost':frost?', frosted':''));
  }
  $('moves').textContent=rules.mp_moves();$('panes').textContent=rules.mp_left();$('score').textContent=rules.mp_score();
  $('mode').disabled=busy;
  $('rewind').disabled=busy || !rules.mp_can_rewind();
  $('hint').disabled=busy||rules.mp_status()!==0;$('retry').disabled=busy;
}
function rewind() {
  if (busy || !rules.mp_rewind()) return;
  save.history.push('rewind');
  persist();
  selected=-1; hinted=[];
  paint();
  note('Last exchange undone. The board and its moves have been restored.');
  cells.forEach(c=>c.classList.remove('drop','pop'));
  ring(330);
}
$('rewind').onclick=rewind;

function offerRewind() {
  if (!rules.mp_can_rewind()) return;
  const button=$('modalCancel');
  button.hidden=false;
  button.textContent='Undo last move';
  button.onclick=()=>{ $('dialog').close(); rewind(); };
}
function finish() {
  if(rules.mp_status()===2) {
    const last=save.chapter===names.length-1;
    modal('GLASSWORK COMPLETE',names[save.chapter],last?'The final pane is clear. Fit the kingfisher crown to complete the glasshouse.':'Every pane is clear. Return the repaired glass to this part of the pier.',
      'Restore '+names[save.chapter],()=>{
        save.restored=Math.max(save.restored,save.chapter+1);persist();journal();ring(784);
        if(last) complete();else loadChapter(save.chapter+1);
      });
    offerRewind();
  } else if(rules.mp_status()===3) {
    modal('OUT OF MOVES','A fresh start',rules.mp_left()+' frosted panes remain. Try opening a line of four to sweep across the board.','Try again',()=>loadChapter(save.chapter));
    offerRewind();
  }
}
function complete() {
  modal('SIX SECTIONS RESTORED','The glasshouse is open','Sunlight reaches the tide steps again. Your restoration is saved on this device.','Explore the pier',()=>{
    $('rewind').disabled=true;
    note('All six sections restored. Restart a level to play again.');
    $('retry').textContent='↻   Replay campaign';$('retry').onclick=()=>loadChapter(0);
  });
}
function loadChapter(ch) {
  save.chapter=ch;save.history=[];persist();startBoard(ch);selected=-1;hinted=[];journal();paint();controls();
  note(master?'Double rims mark two layers. A line sweep breaks both.':'Match glass on the pale, striped panes to clear the frost.');
}
function controls(){
  $('retry').textContent='↻   Restart level';
  $('retry').onclick=()=>modal('WORKSHOP','Restart this level?','Your moves in this chapter will reset. Earlier restorations are saved.','Restart level',()=>loadChapter(save.chapter),true);
}
async function exchange(a,b) {
  selected=-1;hinted=[];
  if(!rules.mp_swap(a,b)) {
    paint(); cells[b].animate([{transform:'translateX(-4px)'},{transform:'translateX(4px)'},{transform:'none'}],{duration:150});
    note('Make a row or column of three. That swap cost no moves.');return;
  }
  busy=true;
  if (!matchMedia('(prefers-reduced-motion: reduce)').matches) {
    const aBox=cells[a].getBoundingClientRect(), bBox=cells[b].getBoundingClientRect();
    const dx=bBox.left-aBox.left, dy=bBox.top-aBox.top;
    for (const [cell,x,y] of [[cells[a],dx,dy],[cells[b],-dx,-dy]]) {
      cell.firstChild.animate([{transform:'translate('+x+'px,'+y+'px)'},{transform:'none'}],
                              {duration:145,easing:'ease-out'});
    }
  }
  save.history.push([a,b]);persist();paint();ring(392);await sleep(150);
  while(rules.mp_status()===1) {
    const chain=rules.mp_chain();
    note(rules.mp_sweeps()?'Line sweep!':chain>1?'Cascade ×'+chain:'Glass matched!');
    for(const [i,c] of cells.entries())c.classList.toggle('pop',!!rules.mp_clear(i));
    await sleep(185);rules.mp_step();paint();ring(440*Math.pow(1.12,Math.min(chain,6)));
    for(const [i,c] of cells.entries()){
      c.classList.remove('pop');const fall=rules.mp_fall(i);
      c.style.setProperty('--dy',(-Math.min(fall,3)*65)+'%');c.classList.toggle('drop',!!fall);
    }
    await sleep(200);for(const c of cells)c.classList.remove('drop');
  }
  busy=false;paint();
  if(rules.mp_status()===0)note(rules.mp_shuffle()?'Fresh glass! The board was reshuffled.':rules.mp_chain()>1?'Cascade ×'+rules.mp_chain()+' · Keep clearing the striped panes.':'Look for four in a line to sweep a whole row or column.');
  finish();
}
function clickCell(i) {
  if(busy||!rules||rules.mp_status()!==0)return;
  hinted=[];
  if(i===selected)selected=-1;
  else if(selected>=0 && Math.abs(Math.floor(i/6)-Math.floor(selected/6))+Math.abs(i%6-selected%6)===1){void exchange(selected,i);return;}
  else selected=i;
  paint();
}
for(let i=0;i<36;i++){
  const cell=document.createElement('button');cell.className='tile';const img=new Image();img.alt='';img.draggable=false;cell.append(img);
  cell.onclick=()=>{if(!skipClick)clickCell(i);};
  cell.onpointerdown=e=>{skipClick=false;pointer={x:e.clientX,y:e.clientY,i};};
  cell.onpointerup=e=>{
    if(!pointer||busy||rules?.mp_status()!==0)return;
    const {x,y,i:a}=pointer;pointer=null;const dx=e.clientX-x,dy=e.clientY-y;
    if(Math.max(Math.abs(dx),Math.abs(dy))<18)return;
    const b=a+(Math.abs(dx)>Math.abs(dy)?Math.sign(dx):6*Math.sign(dy));
    skipClick=true;if(b>=0&&b<36)void exchange(a,b);
  };
  cell.onpointercancel=()=>{pointer=null;};
  cell.onkeydown=e=>{
    const dirs={ArrowRight:1,ArrowLeft:-1,ArrowDown:6,ArrowUp:-6};
    if(e.key in dirs){e.preventDefault();const next=i+dirs[e.key];if(cells[next]&&(Math.abs(dirs[e.key])===6||Math.floor(i/6)===Math.floor(next/6)))cells[next].focus();}
    if(e.key==='Enter'||e.key===' ')skipClick=false;
  };
  cells.push(cell);$('board').append(cell);
}
$('hint').onclick=()=>{const h=rules.mp_hint();if(h<0)return;selected=-1;hinted=[Math.floor(h/36),h%36];paint();note('Swap the two outlined pieces.');};
$('guide').onclick=()=>modal('HOW TO PLAY','The glazier’s guide','Tap two neighbouring pieces or swipe to swap. Match three over a striped pane to remove frost. Four or more sweep the entire line. Clear every pane before your moves run out. You can undo one exchange per chapter, including its cascades. Use arrows and Space on a keyboard.','Back to the glass',null,true);
$('sound').onclick=async()=>{try{audio??=new AudioContext();await audio.resume();sound=!sound;$('sound').setAttribute('aria-pressed',String(sound));$('sound').textContent=sound?'Sound on':'Sound off';ring(523);}catch{note('Audio is unavailable on this device.');}};
try {
  const bytes=await (await fetch('pier.wasm')).arrayBuffer();
  rules=(await WebAssembly.instantiate(bytes,{})).instance.exports;
  try { master=localStorage.getItem('mosaic-pier-mode')==='master'; } catch {}
  loadJournal();

}catch(e){note('The glass workshop could not load. Refresh to try again.');console.error(e);}

function loadJournal() {
  selected=-1;hinted=[];pointer=null;
  save={chapter:0,restored:0,history:[]};
  $('mode').textContent=master?'Return to story':'Play masterwork';
  $('panes').nextElementSibling.textContent=master?'Frost layers':'Frosted panes';
  try{
    const s=JSON.parse(localStorage.getItem(storageKey()));
    if(s&&Number.isInteger(s.chapter)&&s.chapter>=0&&s.chapter<6&&Number.isInteger(s.restored)&&s.restored>=0&&s.restored<=6&&Array.isArray(s.history))save=s;
  }catch{}
  startBoard(save.chapter);
  const replay=[];for(const p of save.history.slice(0,40)){if(p==='rewind'){if(!rules.mp_rewind())break;replay.push(p);continue;}if(!Array.isArray(p)||p.length!==2||!p.every(Number.isInteger)||!rules.mp_swap(...p))break;while(rules.mp_status()===1)rules.mp_step();replay.push(p);}
  save.history=replay;persist();journal();paint();controls();note(master?'Double rims mark two layers. A line sweep breaks both.':'Match glass on the pale, striped panes to clear the frost.');
  if(save.restored===6&&save.chapter===5&&rules.mp_status()===2)complete();else finish();
}
$('mode').onclick=()=>{
  if(busy || !rules) return;
  master=!master;
  try { localStorage.setItem('mosaic-pier-mode',master?'master':'story'); } catch {}
  loadJournal();
};
