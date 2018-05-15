var createGraph = function (elementName) {
    var graph = new Rickshaw.Graph({
        element: document.getElementById(elementName),
        height: 400,
        renderer: 'area',
        interpolation: 'cardinal',
        unstack: true,
        series: new Rickshaw.Series.FixedDuration([], undefined, {
            timeInterval: 30000,
            maxDataPoints: 200,
            timeBase: new Date().getTime() / 1000
        })
    });

    var ticksTreatment = 'glow';
    var time = new Rickshaw.Fixtures.Time();
    var timeUnit = time.unit('seconds');
    var xAxis = new Rickshaw.Graph.Axis.Time({
        graph: graph,
        timeUnit: timeUnit
    });

    xAxis.render();

    var yAxis = new Rickshaw.Graph.Axis.Y({
        graph: graph,
        tickFormat: Rickshaw.Fixtures.Number.formatKMBT,
        ticksTreatment: ticksTreatment
    });

    yAxis.render();

    return graph;
}

var createLegend = function (elementName, graph) {
    $('#' + elementName).empty();
    var legend = new Rickshaw.Graph.Legend({
        graph: graph,
        element: document.getElementById(elementName)
    });
    var shelving = new Rickshaw.Graph.Behavior.Series.Toggle({
        graph: graph,
        legend: legend
    });
    var highlighter = new Rickshaw.Graph.Behavior.Series.Highlight({
        graph: graph,
        legend: legend
    });
    return legend;
}

window.onload = function () {
    var palette = new Rickshaw.Color.Palette({ scheme: 'cool' });

    var cpuGraph = createGraph("cpu-chart");
    var memGraph = createGraph("mem-chart");

    var cpuHoverDetail = new Rickshaw.Graph.HoverDetail({
        graph: cpuGraph,
        xFormatter: function (x) { return x; },
        yFormatter: function (y) { return numeral(y).format('0.00') + '%' }
    });

    var memHoverDetail = new Rickshaw.Graph.HoverDetail({
        graph: memGraph,
        xFormatter: function (x) { return x; },
        yFormatter: function (y) { return numeral(y).format('0.00b') }
    });

    var updater = function () {
        $.get(document.URL + '/stats',
            function (data) {

                var cpuGraphData = {};
                var memGraphData = {};
                for (var i = 0; i < data.length; i++) {
                    var current = data[i];

                    var formattedCpuUsage = numeral(current.cpuUsagePercentage).format('0.00') + '%';
                    var formattedWorkingMemorySet = numeral(current.workingMemorySet).format('0.00b');

                    if ($('#' + current.name).length) {

                        $('#cpu-' + current.name).text(formattedCpuUsage);
                        $('#mem-' + current.name).text(formattedWorkingMemorySet);
                    } else {
                        $('#overview-table').append("<tr id='" + current.name + "'>" +
                            "<td>" + current.name + "</td>" +
                            "<td id='cpu-" + current.name + "'></td>" +
                            "<td id='mem-" + current.name + "'></td>" +
                            "</tr>");

                        $('#cpu-' + current.name).text(formattedCpuUsage);
                        $('#mem-' + current.name).text(formattedWorkingMemorySet);
                    }

                    cpuGraphData[current.name] = current.cpuUsagePercentage;
                    memGraphData[current.name] = current.workingMemorySet;
                }

                cpuGraph.series.addData(cpuGraphData);
                cpuGraph.render();

                memGraph.series.addData(memGraphData);
                memGraph.render();
            });
    };

    updater();
    setInterval(updater, 2000);
}
