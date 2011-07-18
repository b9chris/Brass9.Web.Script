extend(Component.prototype, {
	say: function(message) {
		var div = this.outputDiv();
		div.innerHTML = message;
	}
});