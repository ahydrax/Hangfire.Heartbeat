"use strict";

var format = function () { return ""; }

var createGraph = function (elementName, yAxisConfig) {
    var graph = new Rickshaw.Graph({
        element: document.getElementById(elementName),
        height: 400,
        renderer: 'area',
        interpolation: 'cardinal',
        unstack: true,
        stroke: true,
        series: new Rickshaw.Series.FixedDuration([{ name: "stub" }], undefined, {
            timeInterval: 3000,
            maxDataPoints: 20,
            timeBase: new Date().getTime() / 60000
        })
    });

    var xAxis = new Rickshaw.Graph.Axis.Time({
        graph: graph,
        ticksTreatment: 'glow',
        timeFixture: new Rickshaw.Fixtures.Time.Local()
    });

    xAxis.render();

    var yAxis = yAxisConfig(graph);

    yAxis.render();

    return graph;
}


var UtilizationViewModel = function () {
    var self = this;

    self.servers = ko.observableArray();
};

var viewModel = new UtilizationViewModel();

window.onload = function () {

    var cpuGraph = createGraph("cpu-chart", function (graph) {
        return new Rickshaw.Graph.Axis.Y({
            graph: graph,
            tickFormat: function (y) { return y !== 0 ? y + '%' : '' },
            ticksTreatment: 'glow',
            min: 0,
            max: 100
        });
    });

    var memGraph = createGraph("mem-chart", function (graph) {
        return new Rickshaw.Graph.Axis.Y({
            graph: graph,
            tickFormat: function (y) { return y !== 0 ? numeral(y).format('0.0b') : ''; },
            ticksTreatment: 'glow'
        });
    });

    var cpuHoverDetail = new Rickshaw.Graph.HoverDetail({
        graph: cpuGraph,
        formatter: function (series, x, y) {
            var date = '<span class="date">' + moment.unix(x) + '</span>';
            var swatch = '<span class="detail_swatch" style="background-color: ' + series.color + '"></span>';
            var content = swatch + series.name + ": " + numeral(y).format('0.00') + '%' + '<br>' + date;
            return content;
        }
    });

    var memHoverDetail = new Rickshaw.Graph.HoverDetail({
        graph: memGraph,
        formatter: function (series, x, y) {
            var date = '<span class="date">' + moment.unix(x)+ '</span>';
            var swatch = '<span class="detail_swatch" style="background-color: ' + series.color + '"></span>';
            var content = swatch + series.name + ": " + numeral(y).format('0.00b') + '<br>' + date;
            return content;
        }
    });

    var updater = function () {
        $.get(document.URL + '/stats',
            function (data) {
                var cpuGraphData = {};
                var memGraphData = {};

                for (var i = 0; i < data.length; i++) {
                    var current = data[i];

                    var server = ko.utils.arrayFirst(viewModel.servers(), function (s) { return s.serverFullName === current.serverFullName; });

                    if (server == null) {
                        server = {
                            serverColor: ko.observable("#000000"),
                            serverName: current.serverName,
                            serverFullName: current.serverFullName,
                            processId: ko.observable(current.processId),
                            processName: ko.observable(current.processName),
                            cpuUsage: ko.observable(current.cpuUsagePercentage + '%'),
                            ramUsage: ko.observable(numeral(current.workingMemorySet).format('0.00b'))
                        };

                        viewModel.servers.push(server);
                    } else {
                        server.processId(current.processId);
                        server.processName(current.processName);
                        server.cpuUsage(current.cpuUsagePercentage + '%');
                        server.ramUsage(numeral(current.workingMemorySet).format('0.00b'));
                    }

                    cpuGraphData[current.serverFullName] = current.cpuUsagePercentage;
                    memGraphData[current.serverFullName] = current.workingMemorySet;

                    var series = ko.utils.arrayFirst(cpuGraph.series, function (s) { return s.name === server.serverFullName });
                    if (series != null) {
                        server.serverColor(series.color);
                    }
                }

                cpuGraph.series.addData(cpuGraphData);
                cpuGraph.render();

                memGraph.series.addData(memGraphData);
                memGraph.render();
            });
    };

    updater();
    setInterval(updater, 3000);
    ko.applyBindings(viewModel);
}
