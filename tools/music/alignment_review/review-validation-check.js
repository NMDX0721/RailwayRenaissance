let rows=[],report={duration:10},exportButton={};
function validate(){let previous=0,valid=rows.length>0;rows.forEach(row=>{let start=row.start.valueAsNumber,end=row.end.valueAsNumber;if(!row.check.checked||!row.text.value.trim()||!Number.isFinite(start)||!Number.isFinite(end)||start<previous||end<=start||end>report.duration)valid=false;previous=end});exportButton.disabled=!valid;}

const row={start:{valueAsNumber:0},end:{valueAsNumber:2},check:{checked:true},text:{value:'fixture'}};
rows=[row];validate();if(exportButton.disabled)throw Error('valid zero start rejected');
row.start.valueAsNumber=NaN;validate();if(!exportButton.disabled)throw Error('empty start accepted');
row.start.valueAsNumber=0;row.end.valueAsNumber=NaN;validate();if(!exportButton.disabled)throw Error('empty end accepted');
row.end.valueAsNumber=2;row.check.checked=false;validate();if(!exportButton.disabled)throw Error('unreviewed cue accepted');
console.log('REVIEW_VALIDATION_REGRESSION_OK');
