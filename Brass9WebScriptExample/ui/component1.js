function Component(name) {
	this.name;
}

function extend(a, b) {
	for(var p in b)
		a[p] = b[p];
}

extend(Component.prototype, {
	outputDiv: function() {
		var div = document.getElementById('output');
		if (div)
			return div;

		div = document.createElement('div');
		div.id = 'output';
		document.body.appendChild(div);
		return div;
	}
});