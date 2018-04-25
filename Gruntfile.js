/// <binding />
module.exports = function (grunt) {

    grunt.initConfig({
        concat: {
            "tablejs": {
                dest: "dist/angara.tablejs.js",
                src: [
                        "src/Angara.TableJS/scripts/Table.header.js",
                        "src/Angara.TableJS/scripts/MathUtils.js",
                        "src/Angara.TableJS/scripts/BandPlot.js",
                        "src/Angara.TableJS/scripts/boxPlot.js",
                        "src/Angara.TableJS/scripts/SelectedCellPlot.js",
                        "src/Angara.TableJS/scripts/PiesPlot.js",
                        "src/Angara.TableJS/scripts/TaskQueue.js",
                        "src/Angara.TableJS/scripts/TableSource.js",
                        "src/Angara.TableJS/scripts/TableViewModel.js",
                        "src/Angara.TableJS/scripts/UI.Tile.js",
                        "src/Angara.TableJS/scripts/UI.TileView.js",
                        "src/Angara.TableJS/scripts/UI.TableView.js",
                        "src/Angara.TableJS/scripts/UI.CorrelationView.js",
                        "src/Angara.TableJS/scripts/UI.TableViewer.js",
                        "src/Angara.TableJS/scripts/Table.footer.js"]
            }
        },
        clean: ["dist/"],
        copy: {
            dist_css: {
                expand: true,
                src: "src/Angara.TableJS/styles/angara.tablejs.css",
                dest: "dist/",
                flatten: true
            },
            dist_img: {
                expand: true,
                src: "src/Angara.TableJS/images/*",
                dest: "dist/img/",
                flatten: true
            },
            dist_deftypes: {
                expand: true,
                src: "src/Angara.TableJS/scripts/angara.tablejs.d.ts",
                dest: "dist/",
                flatten: true
            },
            tablejs: {
                expand: true,
                src: "src/Angara.TableJS/scripts/Table.js",
                dest: "dist/",
                flatten: true
            },
            tableviewjs: {
                expand: true,
                src: "src/Angara.TableJS/scripts/TableView.js",
                dest: "dist/",
                flatten: true
            }
        },
        uglify: {
            options: {
                sourceMap: true
            },
            dist: {
                files: {
                    'dist/angara.tablejs.min.js': ['<%= concat.tablejs.dest %>']
                }
            }
        },
        tsd: {
            refresh: {
                options: {
                    // execute a command
                    command: 'reinstall',

                    //optional: always get from HEAD
                    latest: true,

                    // specify config file
                    config: 'tsd.json',

                    // experimental: options to pass to tsd.API
                    opts: {
                        // props from tsd.Options
                    }
                }
            }
        }
    });

    grunt.loadNpmTasks('grunt-contrib-concat');
    grunt.loadNpmTasks('grunt-contrib-copy');
    grunt.loadNpmTasks('grunt-contrib-uglify');
    grunt.loadNpmTasks('grunt-contrib-clean');
    grunt.loadNpmTasks('grunt-tsd');
    grunt.registerTask('build', ['clean', 'concat', 'uglify', 'copy']);
    grunt.registerTask('default', ['tsd', 'build']);
};