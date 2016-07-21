(function(window, undefined) {
(function(factory) {
	// Define as an AMD module if possible
	if ( typeof define === 'function' && define.amd )
	{
	    define( ['jquery', 'idd', 'jquery.dataTables', 'css!jquery.dataTables-css', 'css!idd-css', 'css!angara.tablejs'], function($, Charting){
			var TableViewer = factory ({}, $, Charting.InteractiveDataDisplay);
			return TableViewer;
		});
	}
	/* Define using browser globals otherwise
	 */
	else if ( window.jQuery && window.InteractiveDataDisplay )
	{
		window.TableViewer = factory({}, window.jQuery, window.InteractiveDataDisplay );
	}
})
(function(TableViewer, $, InteractiveDataDisplay){ // factory, returns "TableViewer"
